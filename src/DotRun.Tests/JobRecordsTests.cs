using DotRun.Database;
using DotRun.Tests.Helpers;
using Xunit;

namespace DotRun.Tests;

public class JobRecordsTests : IntegrationTestBase
{
    [Fact]
    public async Task List_ReturnsSuccessStatusCode()
    {
        // Arrange
        DotRunDbContext context = DbHelper.CreateDbContext();

        // Add test data
        context.Project.Add(new ProjectRecord
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        DbHelper.SaveAndReloadContext(ref context);
    }
}