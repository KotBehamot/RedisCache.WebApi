using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RedisCache.WebApi.Data;
using RedisCache.WebApi.Repositories;
using RedisCache.WebApi.Services;
using System.Reflection;
using StrongTypedCache.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// ProblemDetails and exception handler
builder.Services.AddProblemDetails();

// EF Core InMemory
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
{
    opt.UseInMemoryDatabase("RedisCacheDb");
});

// StrongTypedCache registrations (L1 cache)
// 2 minutes for individual products, 30 seconds for all-products to reduce stale list window
builder.Services.AddStrongTypedInMemoryCache<ProductKey, RedisCache.WebApi.Models.Product>(absoluteExpirationTimeSec: 120);
builder.Services.AddStrongTypedInMemoryCache<AllProductsKey, AllProductsCacheEntry>(absoluteExpirationTimeSec: 30);

// Repository and services DI
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IProductService, ProductService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis");

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Redis Cache Web API",
        Version = "v1",
        Description = "Web API demonstrating Redis cache-aside pattern with EF Core InMemory."
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!await db.Products.AnyAsync())
    {
        var products = SeedData.GetProducts();
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/redis", new HealthCheckOptions
{
    Predicate = r => r.Name == "redis"
});

app.Run();

// Health check class
public class RedisHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ICacheService _cache;

    public RedisHealthCheck(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var available = await _cache.IsAvailableAsync(cancellationToken);
        return available
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Redis is available")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Redis not available");
    }
}

// Seed data helper
internal static class SeedData
{
    public static IEnumerable<RedisCache.WebApi.Models.Product> GetProducts() => new[]
    {
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Laptop Pro 15", Category = "Computers", Price = 1999.99m, Description = "High-end laptop with 15-inch display" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Mechanical Keyboard", Category = "Accessories", Price = 129.99m, Description = "RGB mechanical keyboard" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Wireless Mouse", Category = "Accessories", Price = 49.99m, Description = "Ergonomic wireless mouse" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "4K Monitor", Category = "Monitors", Price = 399.99m, Description = "27-inch 4K UHD monitor" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "USB-C Hub", Category = "Accessories", Price = 59.99m, Description = "7-in-1 USB-C hub" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Noise Cancelling Headphones", Category = "Audio", Price = 299.99m, Description = "Over-ear ANC headphones" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Portable SSD 1TB", Category = "Storage", Price = 149.99m, Description = "High-speed NVMe SSD" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Smartphone XL", Category = "Phones", Price = 1099.00m, Description = "Flagship smartphone" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Tablet 11", Category = "Tablets", Price = 699.00m, Description = "11-inch tablet" },
        new RedisCache.WebApi.Models.Product { Id = RedisCache.WebApi.Models.ProductId.New(), Name = "Gaming Chair", Category = "Furniture", Price = 249.99m, Description = "Ergonomic gaming chair" },
    };
}
