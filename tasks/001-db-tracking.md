## Plan: Implement Workflow Execution Database Tracking with Direct Injection

### Architecture Overview
Create tracking service interfaces that immediately persist state changes to the database. Use direct constructor injection in runners (changed from Singleton to Scoped) to access tracking services. Each workflow run gets its own scoped DbContext through the existing `RunContext` mechanism. If database operations fail, the workflow execution fails - keeping it simple and predictable.

### 1. Database Schema Updates
- Add `LastRunAt` (DateTimeOffset?) to `WorkflowEntity`
- Add `IsActive` (bool, default true) to `WorkflowEntity` 
- Create migration for these changes

### 2. Tracking Service Interfaces (`DotRun.Runtime/Services/Tracking/`)

#### 2.1 **`IWorkflowTrackingService`**
```csharp
public interface IWorkflowTrackingService
{
    // Workflow entity management
    Task<Guid> EnsureWorkflowEntityAsync(string projectName, string workflowName);
    
    // Workflow run management
    Task<Guid> CreateWorkflowRunAsync(Guid workflowId, string name, string slug, string source);
    Task StartWorkflowRunAsync(Guid runId);
    Task CompleteWorkflowRunAsync(Guid runId, ExecutionStatus status);
    
    // Pre-create all execution records
    Task<Dictionary<string, Guid>> CreateExecutionPlanAsync(Guid workflowRunId, Workflow workflow);
}
```

#### 2.2 **`IJobTrackingService`**
```csharp
public interface IJobTrackingService  
{
    Task<Dictionary<string, Guid>> CreateJobRunsAsync(Guid workflowRunId, IList<Job> jobs);
    Task StartJobRunAsync(Guid jobRunId);
    Task CompleteJobRunAsync(Guid jobRunId, ExecutionStatus status);
    Task SkipJobRunAsync(Guid jobRunId);
}
```

#### 2.3 **`IStepTrackingService`**
```csharp
public interface IStepTrackingService
{
    Task<Dictionary<string, Guid>> CreateStepRunsAsync(Guid jobRunId, IList<Step> steps);
    Task StartStepRunAsync(Guid stepRunId);
    Task CompleteStepRunAsync(Guid stepRunId, ExecutionStatus status);
    Task SkipStepRunAsync(Guid stepRunId);
}
```

### 3. Implementation (`DotRun.Database/Services/`)

#### 3.1 **`WorkflowTrackingService`**
```csharp
public class WorkflowTrackingService : IWorkflowTrackingService
{
    private readonly DotRunDbContext _context;
    private readonly ILogger<WorkflowTrackingService> _logger;
    
    public WorkflowTrackingService(DotRunDbContext context, ILogger<WorkflowTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<Guid> EnsureWorkflowEntityAsync(string projectName, string workflowName)
    {
        var workflow = await _context.Workflow
            .FirstOrDefaultAsync(w => w.Project.Name == projectName && w.Name == workflowName);
            
        if (workflow == null)
        {
            var project = await _context.Project.FirstAsync(p => p.Name == projectName);
            workflow = new WorkflowEntity 
            {
                ProjectId = project.Id,
                Name = workflowName,
                Slug = workflowName,
                IsActive = true
            };
            _context.Workflow.Add(workflow);
        }
        
        workflow.LastRunAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(); // Immediate save
        
        return workflow.Id;
    }
    
    public async Task<Dictionary<string, Guid>> CreateExecutionPlanAsync(Guid workflowRunId, Workflow workflow)
    {
        var jobIds = new Dictionary<string, Guid>();
        
        // Pre-create all jobs and steps with Queued status
        foreach (var job in workflow.Jobs)
        {
            var jobRun = new JobRunEntity
            {
                WorkflowRunId = workflowRunId,
                WorkflowId = workflowRunId, // Add this field to track workflow
                Name = job.Name,
                Slug = job.Name,
                Status = ExecutionStatus.Queued
            };
            _context.JobRun.Add(jobRun);
            jobIds[job.Name] = jobRun.Id;
            
            foreach (var step in job.Steps ?? Enumerable.Empty<Step>())
            {
                var stepRun = new StepRunEntity
                {
                    JobRunId = jobRun.Id,
                    Name = step.Name,
                    Slug = step.Name,
                    Status = ExecutionStatus.Queued
                };
                _context.StepRun.Add(stepRun);
            }
        }
        
        await _context.SaveChangesAsync(); // Single save for efficiency
        return jobIds;
    }
    
    public async Task StartWorkflowRunAsync(Guid runId)
    {
        var run = await _context.WorkflowRun.FindAsync(runId);
        run.Status = ExecutionStatus.Started;
        run.StartedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }
    
    public async Task CompleteWorkflowRunAsync(Guid runId, ExecutionStatus status)
    {
        var run = await _context.WorkflowRun.FindAsync(runId);
        run.Status = status;
        run.FinishedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }
}
```

#### 3.2 Similar implementations for `JobTrackingService` and `StepTrackingService`
- Each method calls `SaveChangesAsync()` immediately
- No error handling - let exceptions propagate
- Simple and direct database operations

### 4. Update Runners with Direct Injection

#### 4.1 **`WorkflowRunner`**
```csharp
public class WorkflowRunner : IWorkflowRunner
{
    private readonly IJobRunner _jobRunner;
    private readonly IWorkflowTrackingService _trackingService;
    private readonly ILogger<WorkflowRunner> _logger;

    public WorkflowRunner(
        IJobRunner jobRunner,
        IWorkflowTrackingService trackingService,
        ILogger<WorkflowRunner> logger)
    {
        _jobRunner = jobRunner;
        _trackingService = trackingService;
        _logger = logger;
    }

    public async Task<WorkflowResult> RunAsync(Workflow workflow, RunContext runContext)
    {
        _logger.LogInformation("Starting workflow: {WorkflowName}", workflow.Name);
        
        // Update status - if it fails, workflow fails
        await _trackingService.StartWorkflowRunAsync(runContext.WorkflowRunId);

        var workflowContext = new WorkflowContext(workflow, runContext);
        var results = new List<JobResult>();

        foreach (var job in workflow.Jobs)
        {
            var jobRunId = runContext.JobRunIds[job.Name];
            var jobContext = new JobContext(job, workflowContext, jobRunId);
            
            var result = await _jobRunner.RunAsync(job, jobContext);
            results.Add(result);

            if (result.Failed) break;
        }

        // Complete tracking
        var status = results.Any(r => r.Failed) ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
        await _trackingService.CompleteWorkflowRunAsync(runContext.WorkflowRunId, status);

        return new WorkflowResult(workflowContext) { JobResults = results };
    }
}
```

#### 4.2 **`JobRunner`**
```csharp
public class JobRunner : IJobRunner
{
    private readonly IStepRunner _stepRunner;
    private readonly IJobTrackingService _trackingService;
    private readonly ILogger<JobRunner> _logger;

    public JobRunner(
        IStepRunner stepRunner,
        IJobTrackingService trackingService,
        ILogger<JobRunner> logger)
    {
        _stepRunner = stepRunner;
        _trackingService = trackingService;
        _logger = logger;
    }

    public async Task<JobResult> RunAsync(Job job, JobContext jobContext)
    {
        _logger.LogInformation("Starting job: {JobName}", job.Name);
        
        // Start tracking - if it fails, job fails
        await _trackingService.StartJobRunAsync(jobContext.JobRunId);

        var results = new List<StepResult>();

        // Handle nested jobs
        if (job.Jobs != null && job.Jobs.Count > 0)
        {
            foreach (var subJob in job.Jobs)
            {
                var subJobContext = new JobContext(subJob, jobContext.WorkflowContext, 
                    jobContext.RunContext.JobRunIds[subJob.Name]);
                var subJobResult = await RunAsync(subJob, subJobContext);
                
                results.Add(new StepResult(null) { ExitCode = subJobResult.Failed ? 1 : 0 });
                
                if (subJobResult.Failed) break;
            }
        }
        // Handle steps
        else if (job.Steps != null && job.Steps.Count > 0)
        {
            foreach (var step in job.Steps)
            {
                var stepRunId = jobContext.RunContext.StepRunIds[$"{job.Name}.{step.Name}"];
                var stepContext = new StepContext(step, jobContext, stepRunId);
                
                var result = await _stepRunner.RunAsync(step, stepContext);
                results.Add(result);

                if (result.Failed) break;
            }
        }

        var jobResult = new JobResult(jobContext) { StepResults = results };
        
        // Complete tracking
        var status = jobResult.Failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
        await _trackingService.CompleteJobRunAsync(jobContext.JobRunId, status);

        return jobResult;
    }
}
```

#### 4.3 **`StepRunner`**
```csharp
public class StepRunner : IStepRunner
{
    private readonly IStepTrackingService _trackingService;
    private readonly IStepExecutorFactory _executorFactory;
    private readonly ILogger<StepRunner> _logger;

    public StepRunner(
        IStepTrackingService trackingService,
        IStepExecutorFactory executorFactory,
        ILogger<StepRunner> logger)
    {
        _trackingService = trackingService;
        _executorFactory = executorFactory;
        _logger = logger;
    }

    public async Task<StepResult> RunAsync(Step step, StepContext stepContext)
    {
        _logger.LogInformation("Starting step: {StepName}", step.Name);
        
        // Start tracking - if it fails, step fails
        await _trackingService.StartStepRunAsync(stepContext.StepRunId);

        // Execute step...
        var executor = _executorFactory.Create(step);
        var result = await executor.ExecuteAsync(step, stepContext);

        // Complete tracking
        var status = result.Failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
        await _trackingService.CompleteStepRunAsync(stepContext.StepRunId, status);

        return result;
    }
}
```

### 5. Update `WorkflowExecutor`
```csharp
public class WorkflowExecutor : IWorkflowExecutor
{
    // ... existing dependencies ...

    public async Task<WorkflowResult> ExecuteAsync(
        DotRunConfig config,
        string projectName,
        string workflowName,
        CancellationToken cancellationToken = default)
    {
        // ... validation code ...

        // Create run context with its own scope
        using var runContext = new RunContext(_serviceProvider);
        
        // Get scoped tracking service from the RunContext's scope
        var scopedTracking = runContext.ServiceProvider.GetRequiredService<IWorkflowTrackingService>();
        
        // Ensure workflow entity exists
        var workflowId = await scopedTracking.EnsureWorkflowEntityAsync(projectName, workflowName);
        
        // Get workflow source (YAML content)
        var yamlSource = File.ReadAllText(workflow.WorkflowFilePath);
        
        // Create workflow run
        var workflowRunId = await scopedTracking.CreateWorkflowRunAsync(
            workflowId, workflow.Name, workflow.Name, yamlSource);
        runContext.WorkflowRunId = workflowRunId;
        
        // Pre-create execution plan (returns job/step IDs)
        var jobIds = await scopedTracking.CreateExecutionPlanAsync(workflowRunId, workflow);
        runContext.JobRunIds = jobIds;
        
        // Also pre-populate step IDs if needed
        // runContext.StepRunIds = await GetStepIds...
        
        // Get scoped runner from the RunContext's scope
        var scopedRunner = runContext.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        
        // Execute workflow (runner has its own injected tracking service from same scope)
        var result = await scopedRunner.RunAsync(workflow, runContext);
        
        return result;
    }
}
```

### 6. Update `RunContext`
```csharp
public class RunContext : IDisposable
{
    private readonly IServiceScope _serviceScope;
    
    public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;
    
    // Tracking data shared across runners
    public Guid WorkflowRunId { get; set; }
    public Dictionary<string, Guid> JobRunIds { get; set; } = new();
    public Dictionary<string, Guid> StepRunIds { get; set; } = new();
    
    public RunContext(IServiceProvider serviceProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
    }
    
    public void Dispose() => _serviceScope?.Dispose();
}
```

### 7. Update Contexts
```csharp
public class JobContext
{
    public Job Job { get; }
    public WorkflowContext WorkflowContext { get; }
    public RunContext RunContext { get; }  // Need access to IDs
    public Guid JobRunId { get; }

    public JobContext(Job job, WorkflowContext workflowContext, Guid jobRunId)
    {
        Job = job;
        WorkflowContext = workflowContext;
        RunContext = workflowContext.RunContext;
        JobRunId = jobRunId;
    }
}

public class StepContext
{
    public Step Step { get; }
    public JobContext JobContext { get; }
    public Guid StepRunId { get; }

    public StepContext(Step step, JobContext jobContext, Guid stepRunId)
    {
        Step = step;
        JobContext = jobContext;
        StepRunId = stepRunId;
    }
}
```

### 8. Service Registration Updates
```csharp
public static IServiceCollection AddDotRunServices(this IServiceCollection services)
{
    // Core Services - remain Singleton
    services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
    services.AddSingleton<IProjectResolver, ProjectResolver>();
    services.AddSingleton<IWorkflowExecutor, WorkflowExecutor>();
    
    // Runners - CHANGE TO SCOPED
    services.AddScoped<IWorkflowRunner, WorkflowRunner>();
    services.AddScoped<IJobRunner, JobRunner>();
    services.AddScoped<IStepRunner, StepRunner>();
    
    // Factories - remain Singleton
    services.AddSingleton<INodeFactory, NodeFactory>();
    services.AddSingleton<IShellFactory, ShellFactory>();
    services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();
    
    // Database and tracking - Scoped
    services.AddDbContext<DotRunDbContext>(options => 
        options.UseNpgsql(connectionString), ServiceLifetime.Scoped);
    
    services.AddScoped<IWorkflowTrackingService, WorkflowTrackingService>();
    services.AddScoped<IJobTrackingService, JobTrackingService>();
    services.AddScoped<IStepTrackingService, StepTrackingService>();
    
    return services;
}
```

### 9. Benefits of This Final Approach

- **Simple and predictable** - If database fails, workflow fails
- **Clean dependency injection** - Each component declares its dependencies in constructor
- **No service locator** - No GetRequiredService calls except at scope creation
- **No transactions** - Suitable for long-running workflows  
- **Immediate persistence** - Each status update saved immediately
- **Proper scoping** - Each workflow run has isolated DbContext
- **Clear failure modes** - Database issues are immediately visible
- **Easy debugging** - Stack traces show exactly where tracking failed
- **Testable** - Easy to mock tracking services for unit tests

### 10. Implementation Order
1. Update database schema (add fields to WorkflowEntity)
2. Create tracking service interfaces
3. Implement tracking services (no error handling, let exceptions propagate)
4. Change runners from Singleton to Scoped in ServiceCollectionExtensions
5. Update runners to use constructor injection
6. Update contexts to include IDs
7. Update WorkflowExecutor to coordinate
8. Test with concurrent workflow runs