using System;
using System.IO;
using DotRun.Runtime;
using DotRun.Runtime.Model;
using DotRun.Runtime.Services;
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
        var config = new DotRunConfig
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "main",
                    BasePath = _testRoot,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "local", Path = "local" }
                    }
                },
                new Repository
                {
                    Name = "",  // Root namespace
                    BasePath = localPath,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                }
            }
        };
        
        // Test various resolution patterns using mount paths
        // When searching for "test", it finds it in dev subdirectory via root mount
        AssertWorkflowResolution(config, "dev/test", "", "dev/test");
        // "compile" is in ci/build subdirectory via root mount
        AssertWorkflowResolution(config, "ci/build/compile", "", "ci/build/compile");
        // "root" is at root of mount
        AssertWorkflowResolution(config, "root", "", "root");
        
        // List all workflows
        var allMountPaths = _resolver.ListWorkflowMountPaths(config);
        foreach (var (mountPath, reference) in allMountPaths)
        {
            _output.WriteLine($"Found workflow at mount path '{mountPath}': {reference}");
        }
    }
    
    private void AssertWorkflowResolution(DotRunConfig config, string mountPath, string expectedRepo, string expectedWorkflowPath)
    {
        var result = _resolver.GetWorkflowByMountPath(config, mountPath);
        
        Assert.NotNull(result.Workflow);
        Assert.NotNull(result.Reference);
        Assert.Equal(expectedRepo, result.Reference.Repository);
        
        var workflowName = mountPath.Contains("/") ? mountPath.Substring(mountPath.LastIndexOf('/') + 1) : mountPath;
        Assert.Equal(workflowName, result.Reference.Workflow);
        
        _output.WriteLine($"✓ Mount path '{mountPath}' resolved to reference: {result.Reference}");
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