using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestApiDdd.Infrastructure.Abstractions;
using RestApiDdd.Infrastructure.Caching;
using RestApiDdd.Infrastructure.Configuration;
using RestApiDdd.Infrastructure.Data;
using RestApiDdd.Infrastructure.Events;
using RestApiDdd.Infrastructure.Repositories;
using RestApiDdd.Infrastructure.Resilience;
using RestApiDdd.Service.Abstractions;

namespace RestApiDdd.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var baseConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
        var databaseOptions = configuration
            .GetSection(DatabaseResilienceOptions.SectionName)
            .Get<DatabaseResilienceOptions>() ?? new DatabaseResilienceOptions();
        var connectionString = SqlServerConnectionStringFactory.Build(baseConnectionString, databaseOptions);

        services.AddDbContext<ApplicationDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                        sqlOptions.EnableRetryOnFailure(
                            databaseOptions.MaxRetryCount,
                            TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                            errorNumbersToAdd: null);
                    });
            });
        services.AddMemoryCache();

        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();
        services.AddSingleton(databaseOptions);
        services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
        services.AddScoped<IDatabaseResilienceExecutor, DatabaseResilienceExecutor>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceStatusTypeRepository, ServiceStatusTypeRepository>();

        return services;
    }
}
