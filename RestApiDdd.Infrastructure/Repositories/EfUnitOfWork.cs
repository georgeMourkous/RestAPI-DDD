using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class EfUnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
