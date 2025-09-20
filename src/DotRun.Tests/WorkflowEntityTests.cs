using DotRun.Database;
using DotRun.Tests.Helpers;
using Xunit;

namespace DotRun.Tests;

public class WorkflowEntityTests : IntegrationTestBase
{
    [Fact]
    public async Task List_ReturnsSuccessStatusCode()
    {
        // Arrange
        DotRunDbContext context = DbHelper.CreateDbContext();

        // Add test data
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Project.Add(project);
        
        context.Workflow.Add(new WorkflowEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Test Workflow",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        
        DbHelper.SaveAndReloadContext(ref context);
    }
}