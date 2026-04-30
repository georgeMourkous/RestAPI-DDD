using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Abstractions;

public interface IPackageApplicationService
{
    Task<IReadOnlyList<PackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default);

    Task<PackageDto> GetPackageAsync(int id, CancellationToken cancellationToken = default);

    Task<PackageDto> CreatePackageAsync(CreatePackageDto package, CancellationToken cancellationToken = default);

    Task UpdatePackageAsync(int id, UpdatePackageDto package, CancellationToken cancellationToken = default);

    Task PatchPackageAsync(int id, PatchPackageDto package, CancellationToken cancellationToken = default);

    Task DeletePackageAsync(int id, CancellationToken cancellationToken = default);
}
