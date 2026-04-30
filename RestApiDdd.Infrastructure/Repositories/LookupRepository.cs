using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class LookupRepository(
    ApplicationDbContext dbContext,
    IMemoryCache memoryCache) : ILookupRepository
{
    private const string ServicesCacheKey = "lookups:services";
    private const string PackageCategoriesCacheKey = "lookups:package-categories";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<IReadOnlyList<ServiceLookupDto>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        return await memoryCache.GetOrCreateAsync(ServicesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            return await dbContext.Services
                .AsNoTracking()
                .OrderBy(service => service.Name)
                .Select(service => new ServiceLookupDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description
                })
                .ToListAsync(cancellationToken);
        }) ?? [];
    }

    public async Task<IReadOnlyList<PackageCategoryDto>> GetPackageCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await memoryCache.GetOrCreateAsync(PackageCategoriesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            return await dbContext.PackageCategories
                .AsNoTracking()
                .OrderBy(category => category.SortOrder)
                .ThenBy(category => category.Name)
                .Select(category => new PackageCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    SortOrder = category.SortOrder,
                    Visible = category.Visible
                })
                .ToListAsync(cancellationToken);
        }) ?? [];
    }

    public async Task<bool> PackageCategoryExistsAsync(int packageCategoryId, CancellationToken cancellationToken = default)
    {
        var categories = await GetPackageCategoriesAsync(cancellationToken);
        return categories.Any(category => category.Id == packageCategoryId);
    }

    public async Task<IReadOnlySet<int>> GetExistingServiceIdsAsync(IEnumerable<int> serviceIds, CancellationToken cancellationToken = default)
    {
        var requestedIds = serviceIds.ToHashSet();
        if (requestedIds.Count == 0)
        {
            return new HashSet<int>();
        }

        var services = await GetServicesAsync(cancellationToken);
        return services
            .Where(service => requestedIds.Contains(service.Id))
            .Select(service => service.Id)
            .ToHashSet();
    }
}
