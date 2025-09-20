using Microsoft.EntityFrameworkCore;
using DotRun.Runtime.Model;

namespace DotRun.Database;

public class DotRunDbContext : DbContext
{
    public DbSet<WorkflowEntity> Workflow { get; set; }
    public DbSet<WorkflowRunEntity> WorkflowRun { get; set; }
    public DbSet<JobRunEntity> JobRun { get; set; }
    public DbSet<StepRunEntity> StepRun { get; set; }

    public DotRunDbContext(DbContextOptions<DotRunDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=14080;Database=dotrun;Username=postgres;Password=postgres");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ExecutionStatus>();
        
        // Configure singular table names (remove "Record" suffix)
        modelBuilder.Entity<WorkflowEntity>().ToTable("Workflow");
        modelBuilder.Entity<WorkflowRunEntity>().ToTable("WorkflowRun");
        modelBuilder.Entity<JobRunEntity>().ToTable("JobRun");
        modelBuilder.Entity<StepRunEntity>().ToTable("StepRun");

        // Configure base entity properties for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(EntityBase)))
            {
                modelBuilder.Entity(entityType.ClrType).Property<Guid>("Id")
                    .HasDefaultValueSql("gen_random_uuid()");
                
                modelBuilder.Entity(entityType.ClrType).Property<DateTimeOffset>("CreatedAt")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                modelBuilder.Entity(entityType.ClrType).Property<DateTimeOffset>("UpdatedAt")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        // Configure relationships
        // Workflow no longer has a relationship with Project


        modelBuilder.Entity<WorkflowRunEntity>()
            .HasOne<WorkflowEntity>()
            .WithMany()
            .HasForeignKey(wr => wr.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobRunEntity>()
            .HasOne<WorkflowRunEntity>()
            .WithMany()
            .HasForeignKey(jr => jr.WorkflowRunId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<StepRunEntity>()
            .HasOne<JobRunEntity>()
            .WithMany()
            .HasForeignKey(sr => sr.JobRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure string properties
        modelBuilder.Entity<WorkflowEntity>()
            .Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(255);
            
        modelBuilder.Entity<WorkflowEntity>()
            .Property(w => w.QualifiedPath)
            .IsRequired()
            .HasMaxLength(500);
            
        modelBuilder.Entity<WorkflowEntity>()
            .HasIndex(w => w.QualifiedPath)
            .IsUnique();

        modelBuilder.Entity<JobRunEntity>()
            .Property(j => j.Name)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<StepRunEntity>()
            .Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(255);
    }
}