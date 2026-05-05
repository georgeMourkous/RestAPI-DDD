using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
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
        var currentDirectory = Directory.GetCurrentDirectory();

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(currentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

        AddApiJsonFiles(configBuilder, currentDirectory, environment);

        configBuilder.AddEnvironmentVariables();

        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            configBuilder.AddUserSecrets<ApplicationDbContextFactory>(optional: true);
        }

        var configuration = configBuilder.Build();

        if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            LoadAwsParameterStoreConnectionStringAsync(configuration).GetAwaiter().GetResult();
        }

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

    private static void AddApiJsonFiles(IConfigurationBuilder configBuilder, string currentDirectory, string environment)
    {
        var candidateApiDirectories = new[]
        {
            Path.Combine(currentDirectory, "RestApiDdd.Api"),
            Path.Combine(currentDirectory, "..", "RestApiDdd.Api")
        };

        foreach (var apiDirectory in candidateApiDirectories)
        {
            AddJsonFileIfExists(configBuilder, Path.Combine(apiDirectory, "appsettings.json"));
            AddJsonFileIfExists(configBuilder, Path.Combine(apiDirectory, $"appsettings.{environment}.json"));
        }
    }

    private static void AddJsonFileIfExists(IConfigurationBuilder configBuilder, string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            configBuilder.AddJsonFile(fullPath, optional: false, reloadOnChange: true);
        }
    }

    private static async Task LoadAwsParameterStoreConnectionStringAsync(IConfigurationRoot configuration)
    {
        var options = configuration
            .GetSection(AwsParameterStoreOptions.SectionName)
            .Get<AwsParameterStoreOptions>() ?? new AwsParameterStoreOptions();

        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            throw new InvalidOperationException("AwsParameterStore:Region is required when AwsParameterStore is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.DefaultConnectionParameterName))
        {
            throw new InvalidOperationException("AwsParameterStore:DefaultConnectionParameterName is required when AwsParameterStore is enabled.");
        }

        var region = RegionEndpoint.GetBySystemName(options.Region);
        using var client = new AmazonSimpleSystemsManagementClient(region);
        var response = await client.GetParameterAsync(
            new GetParameterRequest
            {
                Name = options.DefaultConnectionParameterName,
                WithDecryption = true
            });

        configuration["ConnectionStrings:DefaultConnection"] = response.Parameter.Value;
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class AwsParameterStoreOptions
    {
        public const string SectionName = "AwsParameterStore";

        public bool Enabled { get; init; }

        public string? Region { get; init; }

        public string? DefaultConnectionParameterName { get; init; }
    }
}
