using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using RestApiDdd.Domain.Common;
using RestApiDdd.Infrastructure.Abstractions;
using RestApiDdd.Infrastructure.Configuration;

namespace RestApiDdd.Infrastructure.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            configBuilder.AddUserSecrets<ApplicationDbContextFactory>(optional: true);
        }

        var configuration = configBuilder.Build();

        var baseConnectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? configuration["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Configure it in appsettings.json or user secrets for development.");
        }

        var databaseOptions = configuration
            .GetSection(DatabaseResilienceOptions.SectionName)
            .Get<DatabaseResilienceOptions>() ?? new DatabaseResilienceOptions();
        var connectionString = SqlServerConnectionStringFactory.Build(baseConnectionString, databaseOptions);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                sqlOptions.EnableRetryOnFailure(
                    databaseOptions.MaxRetryCount,
                    TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
            });

        return new ApplicationDbContext(optionsBuilder.Options, new NoOpDomainEventDispatcher());
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
