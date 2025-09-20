using DotRun.Database;
using DotRun.Tests.Helpers;
using Xunit;

namespace DotRun.Tests;

public class WorkflowEntityTests : IntegrationTestBase
{
    [Fact]
    public async Task Test_WorkflowEntity_Creation()
    {
        // Arrange
        DotRunDbContext context = DbHelper.CreateDbContext();

        // Add test data
        context.Workflow.Add(new WorkflowEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Workflow",
            Slug = "test-workflow",
            QualifiedPath = "test/workflow",
            YamlFilePath = "/test/workflow.yaml",
            SourceHash = "hash123",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeactivatedAt = null
        });
        
        DbHelper.SaveAndReloadContext(ref context);
        
        // Assert
        var workflow = context.Workflow.FirstOrDefault();
        Assert.NotNull(workflow);
        Assert.Equal("Test Workflow", workflow.Name);
        Assert.Equal("test/workflow", workflow.QualifiedPath);
        Assert.Null(workflow.DeactivatedAt);
    }
}