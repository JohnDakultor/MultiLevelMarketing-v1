using modular_mlm.Infrastructure.Caching;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.Caching;

public sealed class InMemoryCacheTests
{
    [Test]
    public async Task CacheExpiresAndInvalidatesByPrefix()
    {
        var cache = new InMemoryCache(TimeProvider.System);
        await cache.SetAsync("catalog:one:first", "one", TimeSpan.FromMinutes(1), default);
        await cache.SetAsync("catalog:one:second", "two", TimeSpan.FromMinutes(1), default);
        await cache.SetAsync("catalog:two:first", "three", TimeSpan.FromMinutes(1), default);

        await cache.RemoveByPrefixAsync("catalog:one:", default);

        (await cache.GetAsync<string>("catalog:one:first", default)).ShouldBeNull();
        (await cache.GetAsync<string>("catalog:one:second", default)).ShouldBeNull();
        (await cache.GetAsync<string>("catalog:two:first", default)).ShouldBe("three");
    }
}
