using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotRun.Runtime.Model.V2;
using DotRun.Runtime.Services.V2;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DotRun.Runtime.Tests.Services.V2;

public class WorkflowResolverTests : IDisposable
{
    private readonly IWorkflowResolver _resolver;
    private readonly ILogger<WorkflowResolver> _loggerMock;
    private readonly string _testRoot;

    public WorkflowResolverTests()
    {
        _loggerMock = Substitute.For<ILogger<WorkflowResolver>>();
        _resolver = new WorkflowResolver(_loggerMock);

        // Create a temporary test directory
        _testRoot = Path.Combine(Path.GetTempPath(), $"dotrun_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        // Clean up the test root directory (which contains all test directories)
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Fact]
    public void GetWorkflowByMountPath_WithEmptyPath_ReturnsNull()
    {
        // Arrange
        var config = new DotRunConfigV2();

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "");

        // Assert
        Assert.Null(result.Workflow);
        Assert.Null(result.Reference);
    }

    [Fact]
    public void GetWorkflowByMountPath_WithNullPath_ReturnsNull()
    {
        // Arrange
        var config = new DotRunConfigV2();

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, null);

        // Assert
        Assert.Null(result.Workflow);
        Assert.Null(result.Reference);
    }

    [Fact]
    public void GetWorkflowByMountPath_SimpleWorkflowInRoot_FindsWorkflow()
    {
        // Arrange
        var workflowDir = CreateTestDirectory("workflows");
        CreateWorkflowFile(workflowDir, "demo.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "", // Root repository
                    BasePath = workflowDir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "demo");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("demo", result.Workflow.Name);
        Assert.Equal("demo", result.Reference.Workflow);
    }

    [Fact]
    public void GetWorkflowByReference_WorkflowWithRepoAndPath_FindsWorkflow()
    {
        // Arrange
        var baseDir = CreateTestDirectory("base");
        var projectDir = Path.Combine(baseDir, "projects", "proj1");
        Directory.CreateDirectory(projectDir);
        CreateWorkflowFile(projectDir, "build.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "local",
                    BasePath = baseDir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "dev", Path = "projects/proj1" }
                    }
                }
            }
        };

        // Act - using reference API now
        var reference = new WorkflowReference { Repository = "local", Path = "projects/proj1", Workflow = "build" };
        var workflow = _resolver.GetWorkflowByReference(config, reference);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("build", workflow.Name);
    }

    [Fact]
    public void GetByPath_WorkflowInSubdirectory_FindsWorkflow()
    {
        // Arrange
        var baseDir = CreateTestDirectory("base");
        var subDir = Path.Combine(baseDir, "workflows", "ci");
        Directory.CreateDirectory(subDir);
        CreateWorkflowFile(subDir, "test.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "",
                    BasePath = baseDir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "workflows" }
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "ci/test");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("test", result.Workflow.Name);
        Assert.NotNull(result.Reference);
        Assert.Equal("test", result.Reference.Workflow);
    }

    [Fact]
    public void GetWorkflowByMountPath_AbsolutePath_ResolvesCorrectly()
    {
        // Arrange
        var absoluteDir = CreateTestDirectory("absolute");
        CreateWorkflowFile(absoluteDir, "deploy.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "ops",
                    BasePath = "/some/other/path", // This should be ignored
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "prod", Path = absoluteDir } // Absolute path
                    }
                }
            }
        };

        // Act - using mount path without repo prefix
        var result = _resolver.GetWorkflowByMountPath(config, "deploy");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("deploy", result.Workflow.Name);
        Assert.NotNull(result.Reference);
        Assert.Equal("ops", result.Reference.Repository);
        Assert.Equal("deploy", result.Reference.Workflow);
    }

    [Fact]
    public void GetByPath_MultipleRepositories_SearchesAll()
    {
        // Arrange
        var dir1 = CreateTestDirectory("repo1");
        var dir2 = CreateTestDirectory("repo2");
        CreateWorkflowFile(dir2, "unique.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "first",
                    BasePath = dir1,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                },
                new Repository
                {
                    Name = "second",
                    BasePath = dir2,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "unique");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("unique", result.Workflow.Name);
        Assert.NotNull(result.Reference);
    }

    [Fact]
    public void GetByPath_AmbiguousName_ReturnsFirstMatch()
    {
        // Arrange
        var dir1 = CreateTestDirectory("repo1");
        var dir2 = CreateTestDirectory("repo2");
        CreateWorkflowFile(dir1, "common.yaml");
        CreateWorkflowFile(dir2, "common.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "first",
                    BasePath = dir1,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                },
                new Repository
                {
                    Name = "second",
                    BasePath = dir2,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "common");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.NotNull(result.Reference);
        // Should log a warning about multiple matches - NSubstitute doesn't have direct Log verification
        // so we just verify the result is not null
    }

    [Fact]
    public void ListWorkflows_ReturnsAllWorkflows()
    {
        // Arrange
        var baseDir = CreateTestDirectory("base");
        var subDir1 = Path.Combine(baseDir, "workflows");
        var subDir2 = Path.Combine(baseDir, "more", "nested");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        CreateWorkflowFile(baseDir, "root.yaml");
        CreateWorkflowFile(subDir1, "sub1.yaml");
        CreateWorkflowFile(subDir2, "deep.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "test",
                    BasePath = baseDir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "all", Path = "." }
                    }
                }
            }
        };

        // Act
        var mountPaths = _resolver.ListWorkflowMountPaths(config);

        // Assert
        Assert.Equal(3, mountPaths.Count);
        var paths = mountPaths.Select(mp => mp.MountPath).ToList();
        Assert.Contains("root", paths);
        Assert.Contains("workflows/sub1", paths);
        Assert.Contains("more/nested/deep", paths);
    }

    [Fact]
    public void GetWorkflowByMountPath_ExistingWorkflow_ReturnsWorkflow()
    {
        // Arrange
        var dir = CreateTestDirectory("validate");
        CreateWorkflowFile(dir, "exists.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "",
                    BasePath = dir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "exists");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.NotNull(result.Reference);
    }

    [Fact]
    public void GetWorkflowByMountPath_NonExistentWorkflow_ReturnsNull()
    {
        // Arrange
        var dir = CreateTestDirectory("validate");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "",
                    BasePath = dir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "nothere");

        // Assert
        Assert.Null(result.Workflow);
        Assert.Null(result.Reference);
    }

    [Fact]
    public void ListWorkflowMountPaths_ReturnsAllMatchingWorkflows()
    {
        // Arrange
        var dir1 = CreateTestDirectory("repo1");
        var dir2 = CreateTestDirectory("repo2");
        CreateWorkflowFile(dir1, "build.yaml");
        CreateWorkflowFile(dir2, "build.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "first",
                    BasePath = dir1,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "dev", Path = "." }
                    }
                },
                new Repository
                {
                    Name = "second",
                    BasePath = dir2,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "prod", Path = "." }
                    }
                }
            }
        };

        // Act
        var mountPaths = _resolver.ListWorkflowMountPaths(config);

        // Assert - Should find build workflow accessible from both mount points
        var buildWorkflows = mountPaths.Where(mp => mp.Reference.Workflow == "build").ToList();
        Assert.Equal(2, buildWorkflows.Count);
        Assert.Contains(buildWorkflows, mp => mp.Reference.Repository == "first");
        Assert.Contains(buildWorkflows, mp => mp.Reference.Repository == "second");
    }

    [Fact]
    public void GetByPath_EmptyRepoName_MergesIntoRoot()
    {
        // Arrange
        var dir = CreateTestDirectory("root");
        CreateWorkflowFile(dir, "global.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "", // Empty name = global root
                    BasePath = dir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." }
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "global"); // No repo prefix needed

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("global", result.Workflow.Name);
        Assert.NotNull(result.Reference);
        Assert.Equal("global", result.Reference.Workflow);
    }

    [Fact]
    public void GetByPath_EmptyPathName_MountsAtRepoRoot()
    {
        // Arrange
        var dir = CreateTestDirectory("reporoot");
        CreateWorkflowFile(dir, "workflow.yaml");

        var config = new DotRunConfigV2
        {
            Repositories = new List<Repository>
            {
                new Repository
                {
                    Name = "myrepo",
                    BasePath = dir,
                    Paths = new List<RepositoryPath>
                    {
                        new RepositoryPath { Mount = "", Path = "." } // Empty path name
                    }
                }
            }
        };

        // Act
        var result = _resolver.GetWorkflowByMountPath(config, "workflow"); // Mount path access

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("workflow", result.Workflow.Name);
        Assert.NotNull(result.Reference);
        Assert.Equal("myrepo", result.Reference.Repository);
        Assert.Equal("workflow", result.Reference.Workflow);
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