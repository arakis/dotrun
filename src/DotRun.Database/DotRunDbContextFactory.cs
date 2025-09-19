using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotRun.Database;

public class DotRunDbContextFactory : IDesignTimeDbContextFactory<DotRunDbContext>
{
    public DotRunDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DotRunDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=14080;Database=dotrun;Username=postgres;Password=postgres");

        return new DotRunDbContext(optionsBuilder.Options);
    }
}