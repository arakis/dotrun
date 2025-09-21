using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotRun.Runtime.Model;
using DotRun.Runtime.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DotRun.Runtime.Tests.Services.V2;

public class AbsoluteMountTests : IDisposable
{
    private readonly WorkflowResolver _resolver;
    private readonly ILogger<WorkflowResolver> _loggerMock;
    private readonly string _testRoot;

    public AbsoluteMountTests()
    {
        _loggerMock = Substitute.For<ILogger<WorkflowResolver>>();
        _resolver = new WorkflowResolver(_loggerMock);
        
        // Create a temporary test directory
        _testRoot = Path.Combine(Path.GetTempPath(), $"dotrun_absolute_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        // Clean up the test root directory
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Fact]
    public void AbsoluteMount_IgnoresRepositoryName()
    {
        // Arrange
        var dir = CreateTestDirectory("tools");
        CreateWorkflowFile(dir, "deploy.yaml");
        
        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "local",  // This should be ignored
                    BasePath = _testRoot,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "/tools", Path = "tools" }
                    }
                }
            }
        };
        
        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "tools/deploy");
        
        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("deploy", result.Workflow.Name);
        Assert.NotNull(result.Reference);
        Assert.Equal("deploy", result.Reference.Workflow);  // No "local" prefix
    }
    
    [Fact]
    public void MixedMounts_ResolvesCorrectly()
    {
        // Arrange
        var toolsDir = CreateTestDirectory("tools");
        var devDir = CreateTestDirectory("dev");
        CreateWorkflowFile(toolsDir, "shared.yaml");
        CreateWorkflowFile(devDir, "build.yaml");
        
        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "myrepo",
                    BasePath = _testRoot,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "/tools", Path = "tools" },     // Absolute
                        new RepositoryPath { Mount = "dev", Path = "dev" }           // Relative
                    }
                }
            }
        };
        
        // Act & Assert
        // Absolute mount - no repo prefix
        var toolsResult = _resolver.GetWorkflowByMountPath(config, "tools/shared");
        Assert.NotNull(toolsResult.Workflow);
        Assert.NotNull(toolsResult.Reference);
        Assert.Equal("shared", toolsResult.Reference.Workflow);
        
        // Relative mount - accessing through mount path
        var devResult = _resolver.GetWorkflowByMountPath(config, "dev/build");
        Assert.NotNull(devResult.Workflow);
        Assert.NotNull(devResult.Reference);
        Assert.Equal("build", devResult.Reference.Workflow);
    }
    
    [Fact]
    public void NestedAbsoluteMount_WorksCorrectly()
    {
        // Arrange
        var ciDir = CreateTestDirectory("ci");
        var buildsDir = Path.Combine(ciDir, "builds");
        Directory.CreateDirectory(buildsDir);
        CreateWorkflowFile(buildsDir, "test.yaml");
        
        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "repo",
                    BasePath = _testRoot,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "/ci/builds", Path = "ci/builds" }
                    }
                }
            }
        };
        
        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "ci/builds/test");
        
        // Assert
        Assert.NotNull(result.Workflow);
        Assert.NotNull(result.Reference);
        Assert.Equal("test", result.Reference.Workflow);
    }
    
    [Fact]
    public void ListWorkflows_WithAbsoluteMounts()
    {
        // Arrange
        var toolsDir = CreateTestDirectory("tools");
        var localDir = CreateTestDirectory("local");
        CreateWorkflowFile(toolsDir, "deploy.yaml");
        CreateWorkflowFile(localDir, "test.yaml");
        
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
                        new RepositoryPath { Mount = "/shared", Path = "tools" },
                        new RepositoryPath { Mount = "local", Path = "local" }
                    }
                }
            }
        };
        
        // Act
        var mountPaths = _resolver.ListWorkflowMountPaths(config);
        
        // Assert
        Assert.Equal(2, mountPaths.Count);
        var paths = mountPaths.Select(mp => mp.MountPath).ToList();
        Assert.Contains("shared/deploy", paths);      // Absolute mount
        Assert.Contains("local/test", paths);         // Relative mount
    }
    
    [Fact]
    public void RootAbsoluteMount_WorksAsExpected()
    {
        // Arrange
        var rootDir = CreateTestDirectory("root");
        CreateWorkflowFile(rootDir, "global.yaml");
        
        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "ignored",
                    BasePath = _testRoot,
                    Paths = new List<RepositoryPath>
                    {
                        // Mount at absolute root
                        new RepositoryPath { Mount = "/", Path = "root" }
                    }
                }
            }
        };
        
        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "global");
        
        // Assert
        Assert.NotNull(result.Workflow);
        Assert.NotNull(result.Reference);
        Assert.Equal("global", result.Reference.Workflow);  // At root level
    }
    
    // Helper methods
    private string CreateTestDirectory(string name)
    {
        var dir = Path.Combine(_testRoot, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private void CreateWorkflowFile(string directory, string filename)
    {
        var content = @"kind: workflow

jobs:
  test:
    steps:
      - name: test
        node: local
        run: echo test
";

        var filePath = Path.Combine(directory, filename);
        File.WriteAllText(filePath, content);
    }
}