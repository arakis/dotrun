using DotRun;
using DotRun.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testcontainers.PostgreSql;
using Xunit;

public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    protected HttpClient HttpClient;
    protected DbContextHelper DbHelper;

    protected IntegrationTestBase()
    {
        // Create PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("dotrun")
            .WithUsername("postgres")
            .WithPassword("postgres")
            // .WithPortBinding(4090)
            // .WithExposedPort(4090)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the container
        await _postgresContainer.StartAsync();
        await Task.Delay(2000);
        
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove any existing DbContext configurations
                ServiceDescriptor? descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<DotRunDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                ServiceDescriptor? descriptorFactory = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDbContextFactory<DotRunDbContext>));
                if (descriptorFactory != null)
                    services.Remove(descriptorFactory);

                // Remove DbContext configurations
                List<ServiceDescriptor> configurations = services.Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)).ToList();
                foreach (ServiceDescriptor configuration in configurations)
                    services.Remove(configuration);

                // Add PostgreSQL database using the container connection string
                services.AddDbContextFactory<DotRunDbContext>(options =>
                {
                    options.UseNpgsql(_postgresContainer.GetConnectionString());
                });

                builder.UseContentRoot(Path.GetTempPath());
            });
        });

        IDbContextFactory<DotRunDbContext> contextFactory = _factory.Services.GetRequiredService<IDbContextFactory<DotRunDbContext>>();

        // Create the schema and setup initial data
        using DotRunDbContext context = contextFactory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        //JsonHelper.ConfigurJsonOptions(JsonSerializerOptions.Web);
        DbHelper = new DbContextHelper(contextFactory);
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        _factory?.Dispose();
        await _postgresContainer.DisposeAsync();
    }
}