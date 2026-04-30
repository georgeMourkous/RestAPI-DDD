using Microsoft.Extensions.Caching.Memory;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Caching;

internal sealed class MemoryCacheProvider(IMemoryCache memoryCache) : ICacheProvider
{
    public async Task<TItem?> GetOrCreateAsync<TItem>(
        string key,
        Func<CancellationToken, Task<TItem?>> factory,
        TimeSpan absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken = default)
    {
        return await memoryCache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            return await factory(cancellationToken);
        });
    }

    public void Remove(string key)
    {
        memoryCache.Remove(key);
    }
}
