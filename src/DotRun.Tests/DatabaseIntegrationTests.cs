using DotRun.Database;
using DotRun.Tests.Helpers;
using Xunit;

namespace DotRun.Tests;

public class DatabaseIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CanCreateAndQueryProject()
    {
        // Arrange
        await using var context = CreateDbContext();
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        // Act
        context.Project.Add(project);
        await context.SaveChangesAsync();
        
        // Assert
        await using var queryContext = CreateDbContext();
        var savedProject = await queryContext.Project.FindAsync(project.Id);
        
        Assert.NotNull(savedProject);
        Assert.Equal("Test Project", savedProject.Name);
    }
    
    [Fact]
    public async Task CanCreateWorkflowWithRelationships()
    {
        // Arrange
        await using var context = CreateDbContext();
        
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Name = "Project with Workflow",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        var workflow = new WorkflowEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Test Workflow",
            Project = project,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        // Act
        context.Project.Add(project);
        context.Workflow.Add(workflow);
        await context.SaveChangesAsync();
        
        // Assert
        await using var queryContext = CreateDbContext();
        var savedWorkflow = await queryContext.Workflow.FindAsync(workflow.Id);
        
        Assert.NotNull(savedWorkflow);
        Assert.Equal("Test Workflow", savedWorkflow.Name);
        Assert.Equal(project.Id, savedWorkflow.ProjectId);
    }
    
    [Fact]
    public async Task DbHelperWorksCorrectly()
    {
        // Arrange
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Name = "Helper Test Project",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        // Act
        using (var context = DbHelper.CreateDbContext())
        {
            context.Project.Add(project);
            await context.SaveChangesAsync();
        }
        
        // Assert
        using (var context = DbHelper.CreateDbContext())
        {
            var exists = context.Project.Any(p => p.Id == project.Id);
            Assert.True(exists);
        }
    }
}