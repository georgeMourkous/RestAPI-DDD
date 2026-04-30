using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Abstractions;

public interface ILookupRepository
{
    Task<IReadOnlyList<ServiceLookupDto>> GetServicesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageCategoryDto>> GetPackageCategoriesAsync(CancellationToken cancellationToken = default);

    Task<bool> PackageCategoryExistsAsync(int packageCategoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<int>> GetExistingServiceIdsAsync(IEnumerable<int> serviceIds, CancellationToken cancellationToken = default);
}
