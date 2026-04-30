using RestApiDdd.Service.Cqrs;

namespace RestApiDdd.Service.Packages;

public sealed record DeletePackageCommand(int Id) : ICommand<Unit>;
