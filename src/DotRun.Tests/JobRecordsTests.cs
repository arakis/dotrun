using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotRun.Database;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FarmTool.WebServer.Tests.Integration;

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
            CreatedAt = DateTime.UtcNow,
        });
        DbHelper.SaveAndReloadContext(ref context);
    }
}