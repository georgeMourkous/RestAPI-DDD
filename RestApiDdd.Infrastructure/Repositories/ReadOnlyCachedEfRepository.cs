using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Common;
using RestApiDdd.Infrastructure.Abstractions;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Infrastructure.Resilience;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Repositories;

internal abstract class ReadOnlyCachedEfRepository<TEntity>(
    ApplicationDbContext dbContext,
    ICacheProvider cacheProvider,
    IDatabaseResilienceExecutor resilienceExecutor) : IReadOnlyRepository<TEntity>
    where TEntity : Entity
{
    protected ApplicationDbContext DbContext { get; } = dbContext;

    protected virtual TimeSpan CacheDuration => TimeSpan.FromMinutes(30);

    protected virtual string CacheKeyPrefix => typeof(TEntity).FullName ?? typeof(TEntity).Name;

    protected virtual IQueryable<TEntity> CachedQuery => DbContext.Set<TEntity>().AsNoTracking();

    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await resilienceExecutor.ExecuteAsync(
            token => cacheProvider.GetOrCreateAsync(
                GetByIdCacheKey(id),
                innerToken => CachedQuery.FirstOrDefaultAsync(entity => entity.Id == id, innerToken),
                CacheDuration,
                token),
            cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await resilienceExecutor.ExecuteAsync(
            async token => (IReadOnlyList<TEntity>)(await cacheProvider.GetOrCreateAsync(
                ListCacheKey,
                async innerToken => await CachedQuery.ToListAsync(innerToken),
                CacheDuration,
                token) ?? []),
            cancellationToken);
    }

    private string ListCacheKey => $"{CacheKeyPrefix}:list";

    private string GetByIdCacheKey(int id) => $"{CacheKeyPrefix}:id:{id}";
}
