using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using RedisCache.WebApi.Models;
using RedisCache.WebApi.Repositories;
using RedisCache.WebApi.Services;
using StrongTypedCache.Abstractions;

namespace RedisCache.WebApi.Tests.Services;

public class ProductServiceTests
{
    private IFixture _fixture = null!;
    private IProductRepository _repo = null!;
    private ICacheService _cache = null!;
    private ICache<ProductKey, Product> _l1ProductCache = null!;
    private ICache<AllProductsKey, AllProductsCacheEntry> _l1AllProductsCache = null!;
    private ProductService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _repo = Substitute.For<IProductRepository>();
        _cache = Substitute.For<ICacheService>();
        _l1ProductCache = Substitute.For<ICache<ProductKey, Product>>();
        _l1AllProductsCache = Substitute.For<ICache<AllProductsKey, AllProductsCacheEntry>>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProductService>>();
        _sut = new ProductService(_repo, _cache, logger, _l1ProductCache, _l1AllProductsCache);
    }

    [Test]
    public async Task GetAllAsync_ReturnsFromRepository_WhenCacheMiss()
    {
        // L1 miss
        AllProductsCacheEntry? outEntry;
        _l1AllProductsCache.TryGetValue(Arg.Any<AllProductsKey>(), out outEntry!)
            .Returns(ci => { ci[1] = null!; return false; });

        // L2 miss
        _cache.GetAsync<IEnumerable<Product>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IEnumerable<Product>?)null);

        var list = _fixture.CreateMany<Product>(3).ToArray();
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.EqualTo(list));
        await _cache.Received().SetAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Product>>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
        _l1AllProductsCache.Received().CreateEntry(Arg.Any<AllProductsKey>(), Arg.Is<AllProductsCacheEntry>(e => e.Items.SequenceEqual(list)));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsFromCache_WhenL2Hit()
    {
        var p = _fixture.Build<Product>().With(x => x.Id, ProductId.New()).Create();

        // L1 miss
        Product? outProduct;
        _l1ProductCache.TryGetValue(Arg.Any<ProductKey>(), out outProduct!)
            .Returns(ci => { ci[1] = null!; return false; });

        // L2 hit
        _cache.GetAsync<Product>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(p);

        var result = await _sut.GetByIdAsync(p.Id);

        Assert.That(result, Is.EqualTo(p));
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>());
        _l1ProductCache.Received().CreateEntry(Arg.Any<ProductKey>(), p);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsFromL1_WhenPresent()
    {
        var p = _fixture.Build<Product>().With(x => x.Id, ProductId.New()).Create();

        // L1 hit
        Product? outProduct;
        _l1ProductCache.TryGetValue(Arg.Is(new ProductKey(p.Id)), out outProduct!)
            .Returns(ci => { ci[1] = p; return true; });

        var result = await _sut.GetByIdAsync(p.Id);

        Assert.That(result, Is.EqualTo(p));
        await _cache.DidNotReceive().GetAsync<Product>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_Invalidates_L1_and_L2()
    {
        var id = ProductId.New();
        var prod = _fixture.Build<Product>().With(x => x.Id, id).Create();
        _repo.UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(true);

        var ok = await _sut.UpdateAsync(id, prod);

        Assert.That(ok, Is.True);
        await _cache.Received().RemoveAsync(Arg.Is<string>(s => s.StartsWith("products")), Arg.Any<CancellationToken>());
        _l1ProductCache.Received().Remove(new ProductKey(id));
        _l1AllProductsCache.Received().Remove(new AllProductsKey());
    }
}
