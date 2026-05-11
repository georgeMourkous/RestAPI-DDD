using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Entities;
using RestApiDdd.Infrastructure.Abstractions;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Infrastructure.Resilience;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class ServiceStatusTypeRepository(
    ApplicationDbContext dbContext,
    ICacheProvider cacheProvider,
    IDatabaseResilienceExecutor resilienceExecutor)
    : ReadOnlyCachedEfRepository<ServiceStatusType>(dbContext, cacheProvider, resilienceExecutor), IServiceStatusTypeRepository
{
    protected override string CacheKeyPrefix => "service-status-types";

    protected override IQueryable<ServiceStatusType> CachedQuery => DbContext.ServiceStatusTypes
        .AsNoTracking()
        .OrderBy(statusType => statusType.SortOrder)
        .ThenBy(statusType => statusType.Name);
}
