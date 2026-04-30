using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Packages;

internal sealed class PackageApplicationService(
    IQueryHandler<GetPackagesQuery, IReadOnlyList<PackageDto>> getPackagesQueryHandler,
    IQueryHandler<GetPackageByIdQuery, PackageDto> getPackageByIdQueryHandler,
    ICommandHandler<CreatePackageCommand, PackageDto> createPackageCommandHandler,
    ICommandHandler<UpdatePackageCommand, Unit> updatePackageCommandHandler,
    ICommandHandler<PatchPackageCommand, Unit> patchPackageCommandHandler,
    ICommandHandler<DeletePackageCommand, Unit> deletePackageCommandHandler) : IPackageApplicationService
{
    public Task<IReadOnlyList<PackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default)
    {
        return getPackagesQueryHandler.HandleAsync(new GetPackagesQuery(), cancellationToken);
    }

    public Task<PackageDto> GetPackageAsync(int id, CancellationToken cancellationToken = default)
    {
        return getPackageByIdQueryHandler.HandleAsync(new GetPackageByIdQuery(id), cancellationToken);
    }

    public Task<PackageDto> CreatePackageAsync(CreatePackageDto package, CancellationToken cancellationToken = default)
    {
        return createPackageCommandHandler.HandleAsync(new CreatePackageCommand(package), cancellationToken);
    }

    public Task UpdatePackageAsync(int id, UpdatePackageDto package, CancellationToken cancellationToken = default)
    {
        return updatePackageCommandHandler.HandleAsync(new UpdatePackageCommand(id, package), cancellationToken);
    }

    public Task PatchPackageAsync(int id, PatchPackageDto package, CancellationToken cancellationToken = default)
    {
        return patchPackageCommandHandler.HandleAsync(new PatchPackageCommand(id, package), cancellationToken);
    }

    public Task DeletePackageAsync(int id, CancellationToken cancellationToken = default)
    {
        return deletePackageCommandHandler.HandleAsync(new DeletePackageCommand(id), cancellationToken);
    }
}
