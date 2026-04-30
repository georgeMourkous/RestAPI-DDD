using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Service.Abstractions;

public interface IPackageRepository : IRepository<Package>
{
    Task<Package?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Package>> ListWithDetailsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, int? excludedPackageId = null, CancellationToken cancellationToken = default);

    Task<PackageCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageCategory>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    Task<bool> CategoryExistsAsync(int id, CancellationToken cancellationToken = default);

    Task AddCategoryAsync(PackageCategory category, CancellationToken cancellationToken = default);

    void UpdateCategory(PackageCategory category);

    void RemoveCategory(PackageCategory category);
}
