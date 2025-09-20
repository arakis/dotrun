using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace DotRun.Database;

public class DotRunDbContextFactory : IDesignTimeDbContextFactory<DotRunDbContext>
{
    public DotRunDbContext CreateDbContext(string[] args)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=localhost;Port=14080;Database=dotrun;Username=postgres;Password=postgres");
        dataSourceBuilder.MapEnum<ExecutionStatus>();
        var dataSource = dataSourceBuilder.EnableDynamicJson().Build();
        
        var optionsBuilder = new DbContextOptionsBuilder<DotRunDbContext>();
        optionsBuilder.UseNpgsql(dataSource);

        return new DotRunDbContext(optionsBuilder.Options);
    }
}