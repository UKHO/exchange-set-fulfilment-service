using Microsoft.EntityFrameworkCore;
using UKHO.ADDS.EFS.Orchestrator.Dashboard.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Country>().HasKey(x => x.Code);
        modelBuilder.Entity<Country>().OwnsOne(x => x.Medals);
    }
}
