using System;
using System.IO;
using DotRun.Runtime;
using DotRun.Runtime.Model.V2;
using DotRun.Runtime.Services.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace DotRun.Runtime.Tests.Services.V2;

public class WorkflowResolverIntegrationTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly WorkflowResolver _resolver;
    private readonly string _testRoot;

    public WorkflowResolverIntegrationTest(ITestOutputHelper output)
    {
        _output = output;
        _resolver = new WorkflowResolver(NullLogger<WorkflowResolver>.Instance);
        _testRoot = Path.Combine(Path.GetTempPath(), $"dotrun_integration_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Fact]
    public void IntegrationTest_QualifiedPaths()
    {
        // Setup directory structure
        var localPath = Path.Combine(_testRoot, "local");
        var devPath = Path.Combine(localPath, "dev");
        var ciPath = Path.Combine(localPath, "ci", "build");
        Directory.CreateDirectory(devPath);
        Directory.CreateDirectory(ciPath);
        
        CreateWorkflow(devPath, "test.yaml");
        CreateWorkflow(ciPath, "compile.yaml");
        CreateWorkflow(localPath, "root.yaml");
        
        // Create config with repository structure
        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "main",
                    BasePath = _testRoot,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Name = "local", Path = "local" }
                    }
                },
                new Repository
                {
                    Name = "",  // Root namespace
                    BasePath = localPath,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Name = "", Path = "." }
                    }
                }
            }
        };
        
        // Test various resolution patterns
        // When searching for "test", it finds it first in main/local/dev
        AssertWorkflowResolution(config, "test", "main/local/dev/test");
        AssertWorkflowResolution(config, "main/local/dev/test", "main/local/dev/test");
        // "compile" is found first in main/local/ci/build
        AssertWorkflowResolution(config, "compile", "main/local/ci/build/compile");
        // "root" is found in root namespace (empty repo name)
        AssertWorkflowResolution(config, "root", "root");
        
        // List all workflows
        var allWorkflows = _resolver.ListWorkflows(config);
        foreach (var workflow in allWorkflows)
        {
            _output.WriteLine($"Found workflow: {workflow}");
        }
    }
    
    private void AssertWorkflowResolution(DotRunConfigV2 config, string searchPath, string expectedQualifiedPath)
    {
        var result = _resolver.GetByPath(config, searchPath);
        
        Assert.NotNull(result.Workflow);
        Assert.Equal(expectedQualifiedPath, result.QualifiedPath);
        
        _output.WriteLine($"✓ '{searchPath}' resolved to '{result.QualifiedPath}'");
    }
    
    private void CreateWorkflow(string directory, string filename)
    {
        var content = @"kind: workflow

jobs:
  main:
    steps:
      - name: step1
        node: local
        run: echo hello
";
        File.WriteAllText(Path.Combine(directory, filename), content);
    }
}