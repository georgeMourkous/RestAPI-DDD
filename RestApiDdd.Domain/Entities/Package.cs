using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Events;

namespace RestApiDdd.Domain.Entities;

public sealed class Package : AggregateRoot
{
    private readonly List<PackageFrequency> _frequencies = [];
    private readonly List<PackageService> _services = [];

    private Package()
    {
    }

    private Package(
        string name,
        int packageCategoryId,
        string? description,
        DateTime created,
        DateTime? start,
        DateTime? expire,
        bool isQuantityAllowed)
    {
        Created = created;
        UpdateCore(name, packageCategoryId, description, start, expire, isQuantityAllowed);
    }

    public string Name { get; private set; } = string.Empty;

    public int PackageCategoryId { get; private set; }

    public PackageCategory? PackageCategory { get; private set; }

    public string? Description { get; private set; }

    public DateTime Created { get; private set; }

    public DateTime? Start { get; private set; }

    public DateTime? Expire { get; private set; }

    public bool IsQuantityAllowed { get; private set; }

    public IReadOnlyCollection<PackageFrequency> Frequencies => _frequencies.AsReadOnly();

    public IReadOnlyCollection<PackageService> Services => _services.AsReadOnly();

    public static Package Create(
        string name,
        int packageCategoryId,
        string? description,
        DateTime? start,
        DateTime? expire,
        bool isQuantityAllowed,
        IEnumerable<PackageFrequencyDefinition> frequencies,
        IEnumerable<PackageServiceDefinition> services,
        DateTime utcNow)
    {
        var package = new Package(name, packageCategoryId, description, utcNow, start, expire, isQuantityAllowed);
        package.ReplaceFrequencies(frequencies, utcNow);
        package.ReplaceServices(services);

        return package;
    }

    public void Update(
        string name,
        int packageCategoryId,
        string? description,
        DateTime? start,
        DateTime? expire,
        bool isQuantityAllowed,
        IEnumerable<PackageFrequencyDefinition> frequencies,
        IEnumerable<PackageServiceDefinition> services,
        DateTime utcNow)
    {
        var wasActive = IsActiveAt(utcNow);

        UpdateCore(name, packageCategoryId, description, start, expire, isQuantityAllowed);
        ReplaceFrequencies(frequencies, utcNow);
        ReplaceServices(services);

        RaiseActivationChangedIfNeeded(wasActive, utcNow);
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        return (!Start.HasValue || Start.Value <= utcNow)
            && (!Expire.HasValue || Expire.Value >= utcNow);
    }

    private void UpdateCore(
        string name,
        int packageCategoryId,
        string? description,
        DateTime? start,
        DateTime? expire,
        bool isQuantityAllowed)
    {
        Name = Guard.RequiredMaxLength(name, nameof(Name), 255);
        PackageCategoryId = Guard.PositiveId(packageCategoryId, nameof(PackageCategoryId));
        Description = Guard.OptionalMaxLength(description, nameof(Description), 2000);

        if (start.HasValue && expire.HasValue && start.Value > expire.Value)
        {
            throw new DomainException("Package start date cannot be later than expire date.");
        }

        Start = start;
        Expire = expire;
        IsQuantityAllowed = isQuantityAllowed;
    }

    private void ReplaceFrequencies(IEnumerable<PackageFrequencyDefinition> frequencies, DateTime utcNow)
    {
        var requested = frequencies.ToList();
        EnsureUniqueFrequencyNames(requested);

        var requestedIds = requested.Where(frequency => frequency.Id > 0).Select(frequency => frequency.Id).ToHashSet();
        _frequencies.RemoveAll(frequency => frequency.Id > 0 && !requestedIds.Contains(frequency.Id));

        foreach (var requestedFrequency in requested)
        {
            if (requestedFrequency.Id > 0)
            {
                var existing = _frequencies.FirstOrDefault(frequency => frequency.Id == requestedFrequency.Id);
                if (existing is null)
                {
                    throw new DomainException($"Package frequency {requestedFrequency.Id} does not belong to this package.");
                }

                existing.Update(requestedFrequency.Name, requestedFrequency.Frequency, requestedFrequency.IsActive);
                continue;
            }

            _frequencies.Add(PackageFrequency.Create(
                requestedFrequency.Name,
                requestedFrequency.Frequency,
                requestedFrequency.IsActive,
                utcNow));
        }
    }

    private void ReplaceServices(IEnumerable<PackageServiceDefinition> services)
    {
        var requested = services.ToList();
        EnsureUniqueServices(requested);

        var requestedIds = requested.Where(service => service.Id > 0).Select(service => service.Id).ToHashSet();
        _services.RemoveAll(service => service.Id > 0 && !requestedIds.Contains(service.Id));

        foreach (var requestedService in requested)
        {
            if (requestedService.Id > 0)
            {
                var existing = _services.FirstOrDefault(service => service.Id == requestedService.Id);
                if (existing is null)
                {
                    throw new DomainException($"Package service {requestedService.Id} does not belong to this package.");
                }

                existing.Update(
                    requestedService.ServiceId,
                    requestedService.DefaultInstances,
                    requestedService.MinimumInstances,
                    requestedService.MaximumInstances);
                continue;
            }

            _services.Add(PackageService.Create(
                requestedService.ServiceId,
                requestedService.DefaultInstances,
                requestedService.MinimumInstances,
                requestedService.MaximumInstances));
        }
    }

    private void RaiseActivationChangedIfNeeded(bool wasActive, DateTime utcNow)
    {
        var isActive = IsActiveAt(utcNow);
        if (wasActive == isActive)
        {
            return;
        }

        RaiseDomainEvent(new PackageActivationChangedDomainEvent(Id, isActive, utcNow));
    }

    private static void EnsureUniqueFrequencyNames(IEnumerable<PackageFrequencyDefinition> frequencies)
    {
        var duplicatedName = frequencies
            .GroupBy(frequency => frequency.Name?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicatedName is not null)
        {
            throw new DomainException($"Package frequency name '{duplicatedName}' is duplicated.");
        }
    }

    private static void EnsureUniqueServices(IEnumerable<PackageServiceDefinition> services)
    {
        var duplicatedServiceId = services
            .GroupBy(service => service.ServiceId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicatedServiceId.HasValue)
        {
            throw new DomainException($"Service {duplicatedServiceId.Value} is duplicated in this package.");
        }
    }
}
