using AltMediatR.WebApiSample.Domain;
using Microsoft.EntityFrameworkCore;

namespace AltMediatR.WebApiSample.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        base.OnModelCreating(modelBuilder);
    }
}
