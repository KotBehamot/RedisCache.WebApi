using System;
using RedisCache.WebApi.Models;

namespace RedisCache.WebApi.Services;

/// <summary>
/// Strongly-typed cache key for a single product.
/// </summary>
public readonly record struct ProductKey(ProductId Id)
{
    public override string ToString() => Id.Value.ToString("N");
}

/// <summary>
/// Strongly-typed cache key representing the collection of all products.
/// </summary>
public readonly record struct AllProductsKey
{
    public override string ToString() => "ALL";
}
