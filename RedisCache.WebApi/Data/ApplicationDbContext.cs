using Microsoft.EntityFrameworkCore;
using RedisCache.WebApi.Models;

namespace RedisCache.WebApi.Data;

/// <summary>
/// EF Core DbContext using InMemory provider.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
