using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Packages;

public sealed record GetPackagesQuery : IQuery<IReadOnlyList<PackageDto>>;
