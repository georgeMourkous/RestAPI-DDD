using Microsoft.EntityFrameworkCore;
using RestApiDdd.Infrastructure.Abstractions;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Service.Abstractions;
using ServiceAggregate = RestApiDdd.Domain.Entities.Service;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class ServiceRepository(
    ApplicationDbContext dbContext,
    ICacheProvider cacheProvider) : CachedEfRepository<ServiceAggregate>(dbContext, cacheProvider), IServiceRepository
{
    protected override string CacheKeyPrefix => "services";

    protected override IQueryable<ServiceAggregate> CachedQuery => DbContext.Services
        .AsNoTracking()
        .OrderBy(service => service.Name);
}
