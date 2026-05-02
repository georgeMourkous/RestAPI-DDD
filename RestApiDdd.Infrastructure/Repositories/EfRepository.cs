using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Common;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Infrastructure.Resilience;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Repositories;

internal class EfRepository<TEntity>(
    ApplicationDbContext dbContext,
    IDatabaseResilienceExecutor resilienceExecutor) : IRepository<TEntity>
    where TEntity : Entity
{
    protected ApplicationDbContext DbContext { get; } = dbContext;

    protected IDatabaseResilienceExecutor ResilienceExecutor { get; } = resilienceExecutor;

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ResilienceExecutor.ExecuteAsync(
            async token => await DbContext.Set<TEntity>().FindAsync([id], token),
            cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await ResilienceExecutor.ExecuteAsync(
            async token => await DbContext.Set<TEntity>()
                .AsNoTracking()
                .ToListAsync(token),
            cancellationToken);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public virtual void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
    }
}
