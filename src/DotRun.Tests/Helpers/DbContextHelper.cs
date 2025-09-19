using DotRun.Database;
using Microsoft.EntityFrameworkCore;

namespace DotRun.Tests.Helpers;

public class DbContextHelper
{
    private readonly IDbContextFactory<DotRunDbContext> _contextFactory;

    public DbContextHelper(IDbContextFactory<DotRunDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public DotRunDbContext CreateDbContext()
        => _contextFactory.CreateDbContext();

    public DotRunDbContext SaveAndReloadContext(DotRunDbContext context)
    {
        SaveAndReloadContext(ref context);
        return context;
    }

    public void SaveAndReloadContext(ref DotRunDbContext context)
    {
        context.SaveChanges();
        context = _contextFactory.CreateDbContext();
    }
} 