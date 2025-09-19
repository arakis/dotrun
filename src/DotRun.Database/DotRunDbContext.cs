using Microsoft.EntityFrameworkCore;

namespace DotRun.Database;

public class DotRunDbContext : DbContext
{
    public DbSet<ProjectRecord> Project { get; set; }
    public DbSet<WorkflowRecord> Workflow { get; set; }
    public DbSet<JobRecord> Job { get; set; }
    public DbSet<StepRecord> Step { get; set; }
    public DbSet<WorkflowRunRecord> WorkflowRun { get; set; }
    public DbSet<JobRunRecord> JobRun { get; set; }
    public DbSet<StepRunRecord> StepRun { get; set; }

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

        // Configure singular table names (remove "Record" suffix)
        modelBuilder.Entity<ProjectRecord>().ToTable("Project");
        modelBuilder.Entity<WorkflowRecord>().ToTable("Workflow");
        modelBuilder.Entity<JobRecord>().ToTable("Job");
        modelBuilder.Entity<StepRecord>().ToTable("Step");
        modelBuilder.Entity<WorkflowRunRecord>().ToTable("WorkflowRun");
        modelBuilder.Entity<JobRunRecord>().ToTable("JobRun");
        modelBuilder.Entity<StepRunRecord>().ToTable("StepRun");

        // Configure base entity properties for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(RecordBase)))
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
        modelBuilder.Entity<WorkflowRecord>()
            .HasOne(w => w.Project)
            .WithMany()
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobRecord>()
            .HasOne(j => j.Project)
            .WithMany()
            .HasForeignKey(j => j.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StepRecord>()
            .HasOne(s => s.Job)
            .WithMany()
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkflowRunRecord>()
            .HasOne<WorkflowRecord>()
            .WithMany()
            .HasForeignKey(wr => wr.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobRunRecord>()
            .HasOne<WorkflowRunRecord>()
            .WithMany()
            .HasForeignKey(jr => jr.WorkflowRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobRunRecord>()
            .HasOne<JobRecord>()
            .WithMany()
            .HasForeignKey(jr => jr.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StepRunRecord>()
            .HasOne<JobRunRecord>()
            .WithMany()
            .HasForeignKey(sr => sr.JobRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StepRunRecord>()
            .HasOne<StepRecord>()
            .WithMany()
            .HasForeignKey(sr => sr.StepId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string properties
        modelBuilder.Entity<WorkflowRecord>()
            .Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<JobRecord>()
            .Property(j => j.Name)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<StepRecord>()
            .Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<ProjectRecord>()
            .Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(255);
    }
}