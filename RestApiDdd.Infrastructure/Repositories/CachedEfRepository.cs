using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Common;
using RestApiDdd.Infrastructure.Abstractions;
using RestApiDdd.Infrastructure.Data;

namespace RestApiDdd.Infrastructure.Repositories;

internal abstract class CachedEfRepository<TEntity>(
    ApplicationDbContext dbContext,
    ICacheProvider cacheProvider) : EfRepository<TEntity>(dbContext)
    where TEntity : Entity
{
    protected virtual TimeSpan CacheDuration => TimeSpan.FromMinutes(30);

    protected virtual string CacheKeyPrefix => typeof(TEntity).FullName ?? typeof(TEntity).Name;

    protected virtual IQueryable<TEntity> CachedQuery => DbContext.Set<TEntity>().AsNoTracking();

    public override async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await cacheProvider.GetOrCreateAsync(
            GetByIdCacheKey(id),
            token => CachedQuery.FirstOrDefaultAsync(entity => entity.Id == id, token),
            CacheDuration,
            cancellationToken);
    }

    public override async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await cacheProvider.GetOrCreateAsync(
            ListCacheKey,
            async token => await CachedQuery.ToListAsync(token),
            CacheDuration,
            cancellationToken) ?? [];
    }

    public override async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await base.AddAsync(entity, cancellationToken);
        InvalidateListCache();
    }

    public override void Remove(TEntity entity)
    {
        base.Remove(entity);
        InvalidateEntityCache(entity.Id);
        InvalidateListCache();
    }

    protected string ListCacheKey => $"{CacheKeyPrefix}:list";

    protected string GetByIdCacheKey(int id) => $"{CacheKeyPrefix}:id:{id}";

    protected void InvalidateListCache()
    {
        cacheProvider.Remove(ListCacheKey);
    }

    protected void InvalidateEntityCache(int id)
    {
        if (id > 0)
        {
            cacheProvider.Remove(GetByIdCacheKey(id));
        }
    }
}
