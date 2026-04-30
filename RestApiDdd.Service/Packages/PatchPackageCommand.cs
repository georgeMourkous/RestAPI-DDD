using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Packages;

public sealed record PatchPackageCommand(int Id, PatchPackageDto Package) : ICommand<Unit>;
