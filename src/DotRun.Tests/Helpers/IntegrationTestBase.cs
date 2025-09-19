using DotRun.Database;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace DotRun.Tests.Helpers;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private IDbContextFactory<DotRunDbContext> _dbContextFactory = null!;

    protected DbContextHelper DbHelper = null!;

    protected IntegrationTestBase()
    {
        // Create PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("dotrun_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the container
        await _postgresContainer.StartAsync();
        
        // Create DbContext factory
        var connectionString = _postgresContainer.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<DotRunDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        _dbContextFactory = new TestDbContextFactory(optionsBuilder.Options);
        
        // Create the schema and setup initial data
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
        
        DbHelper = new DbContextHelper(_dbContextFactory);
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }
    
    protected DotRunDbContext CreateDbContext()
    {
        return _dbContextFactory.CreateDbContext();
    }
    
    protected async Task<DotRunDbContext> CreateDbContextAsync()
    {
        return await _dbContextFactory.CreateDbContextAsync();
    }

    // Inner class for test DbContext factory
    private class TestDbContextFactory : IDbContextFactory<DotRunDbContext>
    {
        private readonly DbContextOptions<DotRunDbContext> _options;

        public TestDbContextFactory(DbContextOptions<DotRunDbContext> options)
        {
            _options = options;
        }

        public DotRunDbContext CreateDbContext()
        {
            return new DotRunDbContext(_options);
        }

        public async Task<DotRunDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(CreateDbContext());
        }
    }
}