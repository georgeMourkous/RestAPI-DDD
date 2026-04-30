using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Packages;

public sealed record CreatePackageCommand(CreatePackageDto Package) : ICommand<PackageDto>;
