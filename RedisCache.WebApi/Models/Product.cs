using System.ComponentModel.DataAnnotations;

namespace RedisCache.WebApi.Models;

/// <summary>
/// Represents a product in the catalog.
/// </summary>
public class Product
{
    /// <summary>
    /// Product identifier.
    /// </summary>
    [Key]
    public ProductId Id { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Product price.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>
    /// Product category.
    /// </summary>
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Date created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
