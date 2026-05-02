using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;
using RestApiDdd.Infrastructure.Abstractions;
using ServiceAggregate = RestApiDdd.Domain.Entities.Service;

namespace RestApiDdd.Infrastructure.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventDispatcher domainEventDispatcher) : DbContext(options)
{
    public DbSet<ServiceAggregate> Services => Set<ServiceAggregate>();

    public DbSet<PackageCategory> PackageCategories => Set<PackageCategory>();

    public DbSet<Package> Packages => Set<Package>();

    public DbSet<PackageFrequency> PackageFrequencies => Set<PackageFrequency>();

    public DbSet<PackageService> PackageServices => Set<PackageService>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToArray();

        var domainEvents = aggregates
            .SelectMany(entity => entity.DomainEvents)
            .ToArray();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (domainEvents.Length > 0)
        {
            await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
