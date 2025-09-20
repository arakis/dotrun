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
    
    // Pre-create all execution records with provided IDs
    Task CreateExecutionPlanAsync(Guid workflowRunId, Guid workflowId, WorkflowContext workflowContext);
}
```

#### 2.2 **`IJobTrackingService`**
```csharp
public interface IJobTrackingService  
{
    Task CreateJobRunAsync(Guid jobRunId, Guid workflowRunId, Guid workflowId, string name);
    Task StartJobRunAsync(Guid jobRunId);
    Task CompleteJobRunAsync(Guid jobRunId, ExecutionStatus status);
    Task SkipJobRunAsync(Guid jobRunId);
}
```

#### 2.3 **`IStepTrackingService`**
```csharp
public interface IStepTrackingService
{
    Task CreateStepRunAsync(Guid stepRunId, Guid jobRunId, string name);
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
    
    public async Task CreateExecutionPlanAsync(Guid workflowRunId, Guid workflowId, WorkflowContext workflowContext)
    {
        // Pre-create all jobs and steps with Queued status using IDs from contexts
        foreach (var jobContext in workflowContext.JobContexts)
        {
            var jobRun = new JobRunEntity
            {
                Id = jobContext.JobRunId, // Use the ID already assigned to the context
                WorkflowRunId = workflowRunId,
                WorkflowId = workflowId,
                Name = jobContext.Job.Name, // Can be null
                Slug = jobContext.Job.Name, // Can be null
                Status = ExecutionStatus.Queued
            };
            _context.JobRun.Add(jobRun);
            
            foreach (var stepContext in jobContext.StepContexts)
            {
                var stepRun = new StepRunEntity
                {
                    Id = stepContext.StepRunId, // Use the ID already assigned to the context
                    JobRunId = jobContext.JobRunId,
                    Name = stepContext.Step.Name, // Can be null
                    Slug = stepContext.Step.Name, // Can be null
                    Status = ExecutionStatus.Queued
                };
                _context.StepRun.Add(stepRun);
            }
        }
        
        await _context.SaveChangesAsync(); // Single save for efficiency
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

    public async Task<WorkflowResult> RunAsync(Workflow workflow, WorkflowContext workflowContext)
    {
        _logger.LogInformation("Starting workflow: {WorkflowName}", workflow.Name);
        
        // Update status - if it fails, workflow fails
        await _trackingService.StartWorkflowRunAsync(workflowContext.WorkflowRunId);

        var results = new List<JobResult>();

        foreach (var jobContext in workflowContext.JobContexts)
        {
            var result = await _jobRunner.RunAsync(jobContext.Job, jobContext);
            results.Add(result);

            if (result.Failed) break;
        }

        // Complete tracking
        var status = results.Any(r => r.Failed) ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
        await _trackingService.CompleteWorkflowRunAsync(workflowContext.WorkflowRunId, status);

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
        if (jobContext.SubJobContexts != null && jobContext.SubJobContexts.Count > 0)
        {
            foreach (var subJobContext in jobContext.SubJobContexts)
            {
                var subJobResult = await RunAsync(subJobContext.Job, subJobContext);
                
                results.Add(new StepResult(null) { ExitCode = subJobResult.Failed ? 1 : 0 });
                
                if (subJobResult.Failed) break;
            }
        }
        // Handle steps
        else if (jobContext.StepContexts != null && jobContext.StepContexts.Count > 0)
        {
            foreach (var stepContext in jobContext.StepContexts)
            {
                var result = await _stepRunner.RunAsync(stepContext.Step, stepContext);
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
        
        // Build the entire context tree with IDs upfront
        var workflowContext = BuildWorkflowContext(workflow, workflowRunId, runContext);
        
        // Pre-create all execution records using the context tree
        await scopedTracking.CreateExecutionPlanAsync(workflowRunId, workflowId, workflowContext);
        
        // Get scoped runner from the RunContext's scope
        var scopedRunner = runContext.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        
        // Execute workflow with the pre-built context
        var result = await scopedRunner.RunAsync(workflow, workflowContext);
        
        return result;
    }
    
    private WorkflowContext BuildWorkflowContext(Workflow workflow, Guid workflowRunId, RunContext runContext)
    {
        var workflowContext = new WorkflowContext(workflow, runContext)
        {
            WorkflowRunId = workflowRunId,
            JobContexts = new List<JobContext>()
        };
        
        foreach (var job in workflow.Jobs)
        {
            var jobContext = BuildJobContext(job, workflowContext);
            workflowContext.JobContexts.Add(jobContext);
        }
        
        return workflowContext;
    }
    
    private JobContext BuildJobContext(Job job, WorkflowContext workflowContext)
    {
        var jobContext = new JobContext(job, workflowContext)
        {
            JobRunId = Guid.NewGuid() // Assign ID at initialization
        };
        
        // Handle nested jobs
        if (job.Jobs != null && job.Jobs.Count > 0)
        {
            jobContext.SubJobContexts = new List<JobContext>();
            foreach (var subJob in job.Jobs)
            {
                var subJobContext = BuildJobContext(subJob, workflowContext);
                jobContext.SubJobContexts.Add(subJobContext);
            }
        }
        // Handle steps
        else if (job.Steps != null && job.Steps.Count > 0)
        {
            jobContext.StepContexts = new List<StepContext>();
            foreach (var step in job.Steps)
            {
                var stepContext = new StepContext(step, jobContext)
                {
                    StepRunId = Guid.NewGuid() // Assign ID at initialization
                };
                jobContext.StepContexts.Add(stepContext);
            }
        }
        
        return jobContext;
    }
}
```

### 6. Update `RunContext`
```csharp
public class RunContext : IDisposable
{
    private readonly IServiceScope _serviceScope;
    
    public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;
    
    // No need for ID dictionaries - IDs are stored directly in contexts
    
    public RunContext(IServiceProvider serviceProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
    }
    
    public void Dispose() => _serviceScope?.Dispose();
}
```

### 7. Update Contexts
```csharp
public class WorkflowContext
{
    public Workflow Workflow { get; }
    public RunContext RunContext { get; }
    public Guid WorkflowRunId { get; set; }
    
    // Pre-built tree of job contexts
    public List<JobContext> JobContexts { get; set; }

    public WorkflowContext(Workflow workflow, RunContext runContext)
    {
        Workflow = workflow;
        RunContext = runContext;
    }
}

public class JobContext
{
    public Job Job { get; }
    public WorkflowContext WorkflowContext { get; }
    public Guid JobRunId { get; set; } // Assigned at initialization
    
    // Either sub-jobs or steps, not both
    public List<JobContext> SubJobContexts { get; set; }
    public List<StepContext> StepContexts { get; set; }

    public JobContext(Job job, WorkflowContext workflowContext)
    {
        Job = job;
        WorkflowContext = workflowContext;
    }
}

public class StepContext
{
    public Step Step { get; }
    public JobContext JobContext { get; }
    public Guid StepRunId { get; set; } // Assigned at initialization

    public StepContext(Step step, JobContext jobContext)
    {
        Step = step;
        JobContext = jobContext;
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

### 9. Benefits of This ID-Centric Approach

- **ID-centric design** - Entity IDs assigned at context initialization, not lookup by name
- **No name dependencies** - Works even with null/empty names and slugs
- **Pre-built context tree** - Complete execution plan with IDs created before execution starts
- **Simple and predictable** - If database fails, workflow fails
- **Clean dependency injection** - Each component declares its dependencies in constructor
- **No service locator** - No GetRequiredService calls except at scope creation
- **No transactions** - Suitable for long-running workflows  
- **Immediate persistence** - Each status update saved immediately
- **Proper scoping** - Each workflow run has isolated DbContext
- **Clear failure modes** - Database issues are immediately visible
- **Easy debugging** - Stack traces show exactly where tracking failed
- **Testable** - Easy to mock tracking services for unit tests
- **Type-safe ID passing** - IDs stored in strongly-typed context properties

### 10. Implementation Order
1. Update database schema (add LastRunAt and IsActive fields to WorkflowEntity)
2. Update context classes to include ID properties and child context collections
3. Create ID-centric tracking service interfaces
4. Implement tracking services that use context-provided IDs
5. Change runners from Singleton to Scoped in ServiceCollectionExtensions  
6. Update runners to work with pre-built context trees
7. Implement BuildWorkflowContext methods in WorkflowExecutor
8. Update WorkflowExecutor to build context tree before execution
9. Test with concurrent workflow runs to ensure proper isolation