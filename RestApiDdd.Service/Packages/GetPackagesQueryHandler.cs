using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Mapping;

namespace RestApiDdd.Service.Packages;

internal sealed class GetPackagesQueryHandler(
    IPackageRepository packageRepository,
    IClock clock) : IQueryHandler<GetPackagesQuery, IReadOnlyList<PackageDto>>
{
    public async Task<IReadOnlyList<PackageDto>> HandleAsync(GetPackagesQuery query, CancellationToken cancellationToken = default)
    {
        var packages = await packageRepository.ListWithDetailsAsync(cancellationToken);
        return packages.Select(package => package.ToDto(clock.UtcNow)).ToArray();
    }
}
