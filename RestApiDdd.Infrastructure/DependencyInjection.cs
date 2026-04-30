using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Infrastructure.Events;
using RestApiDdd.Infrastructure.Repositories;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddMemoryCache();

        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();

        return services;
    }
}
