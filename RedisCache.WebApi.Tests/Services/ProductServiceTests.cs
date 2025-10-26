using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using RedisCache.WebApi.Models;
using RedisCache.WebApi.Repositories;
using RedisCache.WebApi.Services;

namespace RedisCache.WebApi.Tests.Services;

public class ProductServiceTests
{
    private IFixture _fixture = null!;
    private IProductRepository _repo = null!;
    private ICacheService _cache = null!;
    private ProductService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _repo = Substitute.For<IProductRepository>();
        _cache = Substitute.For<ICacheService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProductService>>();
        _sut = new ProductService(_repo, _cache, logger);
    }

    [Test]
    public async Task GetAllAsync_ReturnsFromRepository_WhenCacheMiss()
    {
        _cache.GetAsync<IEnumerable<Product>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IEnumerable<Product>?)null);
        var list = _fixture.CreateMany<Product>(3).ToArray();
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.EqualTo(list));
        await _cache.Received().SetAsync(Arg.Any<string>(), list, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetByIdAsync_ReturnsFromCache_WhenHit()
    {
        var p = _fixture.Build<Product>().With(x => x.Id, Guid.NewGuid()).Create();
        _cache.GetAsync<Product>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(p);

        var result = await _sut.GetByIdAsync(p.Id);

        Assert.That(result, Is.EqualTo(p));
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
