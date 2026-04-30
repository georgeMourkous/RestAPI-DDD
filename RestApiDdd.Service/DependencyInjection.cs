using Microsoft.Extensions.DependencyInjection;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Packages;

namespace RestApiDdd.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPackageApplicationService, PackageApplicationService>();

        services.AddScoped<IQueryHandler<GetPackagesQuery, IReadOnlyList<PackageDto>>, GetPackagesQueryHandler>();
        services.AddScoped<IQueryHandler<GetPackageByIdQuery, PackageDto>, GetPackageByIdQueryHandler>();
        services.AddScoped<ICommandHandler<CreatePackageCommand, PackageDto>, CreatePackageCommandHandler>();
        services.AddScoped<ICommandHandler<UpdatePackageCommand, Unit>, UpdatePackageCommandHandler>();
        services.AddScoped<ICommandHandler<PatchPackageCommand, Unit>, PatchPackageCommandHandler>();
        services.AddScoped<ICommandHandler<DeletePackageCommand, Unit>, DeletePackageCommandHandler>();

        return services;
    }
}
