using DotRun.Database;
using DotRun.Tests.Helpers;
using Xunit;

namespace DotRun.Tests;

public class DatabaseIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CanCreateAndQueryWorkflow()
    {
        // Arrange
        await using var context = CreateDbContext();
        var workflow = new WorkflowEntity
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
        };
        
        // Act
        context.Workflow.Add(workflow);
        await context.SaveChangesAsync();
        
        // Assert
        await using var queryContext = CreateDbContext();
        var savedWorkflow = await queryContext.Workflow.FindAsync(workflow.Id);
        
        Assert.NotNull(savedWorkflow);
        Assert.Equal("Test Workflow", savedWorkflow.Name);
        Assert.Equal("test/workflow", savedWorkflow.QualifiedPath);
        Assert.Null(savedWorkflow.DeactivatedAt);
    }
    
    [Fact]
    public async Task CanDeactivateAndReactivateWorkflow()
    {
        // Arrange
        await using var context = CreateDbContext();
        
        var workflow = new WorkflowEntity
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
        };
        
        // Act - Create active workflow
        context.Workflow.Add(workflow);
        await context.SaveChangesAsync();
        
        // Deactivate workflow
        workflow.DeactivatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        
        // Assert - Check deactivation
        await using var queryContext1 = CreateDbContext();
        var deactivatedWorkflow = await queryContext1.Workflow.FindAsync(workflow.Id);
        Assert.NotNull(deactivatedWorkflow);
        Assert.NotNull(deactivatedWorkflow.DeactivatedAt);
        
        // Reactivate workflow
        await using var updateContext = CreateDbContext();
        var workflowToReactivate = await updateContext.Workflow.FindAsync(workflow.Id);
        workflowToReactivate.DeactivatedAt = null;
        await updateContext.SaveChangesAsync();
        
        // Assert - Check reactivation
        await using var queryContext2 = CreateDbContext();
        var reactivatedWorkflow = await queryContext2.Workflow.FindAsync(workflow.Id);
        Assert.NotNull(reactivatedWorkflow);
        Assert.Null(reactivatedWorkflow.DeactivatedAt);
    }
    
    [Fact]
    public async Task DbHelperWorksCorrectly()
    {
        // Arrange
        var workflow = new WorkflowEntity
        {
            Id = Guid.NewGuid(),
            Name = "Helper Test Workflow",
            Slug = "helper-test",
            QualifiedPath = "helper/test",
            YamlFilePath = "/helper/test.yaml",
            SourceHash = "hash456",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        // Act
        using (var context = DbHelper.CreateDbContext())
        {
            context.Workflow.Add(workflow);
            await context.SaveChangesAsync();
        }
        
        // Assert
        using (var context = DbHelper.CreateDbContext())
        {
            var exists = context.Workflow.Any(w => w.Id == workflow.Id);
            Assert.True(exists);
        }
    }
}