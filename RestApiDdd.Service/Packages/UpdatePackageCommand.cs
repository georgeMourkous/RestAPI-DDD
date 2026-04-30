using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Packages;

public sealed record UpdatePackageCommand(int Id, UpdatePackageDto Package) : ICommand<Unit>;
