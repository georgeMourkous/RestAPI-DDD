namespace RestApiDdd.Service.Abstractions;

public interface ICacheProvider
{
    Task<TItem?> GetOrCreateAsync<TItem>(
        string key,
        Func<CancellationToken, Task<TItem?>> factory,
        TimeSpan absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken = default);

    void Remove(string key);
}
