using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Service.Abstractions;

public interface IPackageRepository : IRepository<Package>
{
    Task<Package?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Package>> ListWithDetailsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, int? excludedPackageId = null, CancellationToken cancellationToken = default);
}
