using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Infrastructure.Resilience;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class EfUnitOfWork(
    ApplicationDbContext dbContext,
    IDatabaseResilienceExecutor resilienceExecutor) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return resilienceExecutor.ExecuteAsync(token => dbContext.SaveChangesAsync(token), cancellationToken);
    }
}
