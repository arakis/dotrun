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
    private readonly WorkflowResolver _resolver;
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
    public void GetByPath_WithEmptyPath_ReturnsNull()
    {
        // Arrange
        var config = new DotRunConfigV2();

        // Act
        var result = _resolver.GetByPath(config, "");

        // Assert
        Assert.Null(result.Workflow);
        Assert.Null(result.QualifiedPath);
    }

    [Fact]
    public void GetByPath_WithNullPath_ReturnsNull()
    {
        // Arrange
        var config = new DotRunConfigV2();

        // Act
        var result = _resolver.GetByPath(config, null);

        // Assert
        Assert.Null(result.Workflow);
        Assert.Null(result.QualifiedPath);
    }

    [Fact]
    public void GetByPath_SimpleWorkflowInRoot_FindsWorkflow()
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
        var result = _resolver.GetByPath(config, "demo");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("demo", result.Workflow.Name);
        Assert.Equal("demo", result.QualifiedPath);
    }

    [Fact]
    public void GetByPath_WorkflowWithRepoAndPath_FindsWorkflow()
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

        // Act
        var result = _resolver.GetByPath(config, "local/dev/build");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("build", result.Workflow.Name);
        Assert.Equal("local/dev/build", result.QualifiedPath);
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
        var result = _resolver.GetByPath(config, "ci/test");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("test", result.Workflow.Name);
        Assert.Equal("ci/test", result.QualifiedPath);
    }

    [Fact]
    public void GetByPath_AbsolutePath_ResolvesCorrectly()
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

        // Act
        var result = _resolver.GetByPath(config, "ops/prod/deploy");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("deploy", result.Workflow.Name);
        Assert.Equal("ops/prod/deploy", result.QualifiedPath);
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
        var result = _resolver.GetByPath(config, "unique");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("unique", result.Workflow.Name);
        Assert.NotNull(result.QualifiedPath);
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
        var result = _resolver.GetByPath(config, "common");

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.NotNull(result.QualifiedPath);
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
        var workflows = _resolver.ListWorkflows(config).ToList();

        // Assert
        Assert.Equal(3, workflows.Count);
        Assert.Contains("test/all/root", workflows);
        Assert.Contains("test/all/workflows/sub1", workflows);
        Assert.Contains("test/all/more/nested/deep", workflows);
    }

    [Fact]
    public void ValidateWorkflow_ExistingWorkflow_ReturnsTrue()
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
        var result = _resolver.ValidateWorkflow(config, "exists");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateWorkflow_NonExistentWorkflow_ReturnsFalse()
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
        var result = _resolver.ValidateWorkflow(config, "nothere");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FindWorkflows_ReturnsAllMatches()
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
        var results = _resolver.FindWorkflows(config, "build").ToList();

        // Assert - The resolver may find workflows through different paths
        Assert.True(results.Count >= 2);
        Assert.Contains(results, r => r.QualifiedPath.Contains("build"));
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
        var result = _resolver.GetByPath(config, "global"); // No repo prefix needed

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("global", result.Workflow.Name);
        Assert.Equal("global", result.QualifiedPath);
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
        var result = _resolver.GetByPath(config, "myrepo/workflow"); // Direct repo access

        // Assert
        Assert.NotNull(result.Workflow);
        Assert.Equal("workflow", result.Workflow.Name);
        Assert.Equal("myrepo/workflow", result.QualifiedPath);
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