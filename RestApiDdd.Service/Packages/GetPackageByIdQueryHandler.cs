using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Mapping;

namespace RestApiDdd.Service.Packages;

internal sealed class GetPackageByIdQueryHandler(
    IPackageRepository packageRepository,
    IClock clock) : IQueryHandler<GetPackageByIdQuery, PackageDto>
{
    public async Task<PackageDto> HandleAsync(GetPackageByIdQuery query, CancellationToken cancellationToken = default)
    {
        var package = await packageRepository.GetByIdWithDetailsAsync(query.Id, cancellationToken);
        if (package is null)
        {
            throw new NotFoundException($"Package {query.Id} was not found.");
        }

        return package.ToDto(clock.UtcNow);
    }
}
