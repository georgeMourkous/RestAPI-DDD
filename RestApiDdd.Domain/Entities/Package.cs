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
        bool isQuantityAllowed,
        bool fullPeriod,
        bool postPaid)
    {
        Created = created;
        UpdateCore(name, packageCategoryId, description, start, expire, isQuantityAllowed, fullPeriod, postPaid);
    }

    public string Name { get; private set; } = string.Empty;

    public int PackageCategoryId { get; private set; }

    public PackageCategoryType PackageCategoryType => (PackageCategoryType)PackageCategoryId;

    public PackageCategory? PackageCategory { get; private set; }

    public string? Description { get; private set; }

    public DateTime Created { get; private set; }

    public DateTime? Start { get; private set; }

    public DateTime? Expire { get; private set; }

    public bool IsQuantityAllowed { get; private set; }

    public bool FullPeriod { get; private set; }

    public bool PostPaid { get; private set; }

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
        DateTime utcNow,
        bool fullPeriod = false,
        bool postPaid = false)
    {
        var package = new Package(name, packageCategoryId, description, utcNow, start, expire, isQuantityAllowed, fullPeriod, postPaid);
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
        DateTime utcNow,
        bool fullPeriod = false,
        bool postPaid = false)
    {
        var wasActive = IsActiveAt(utcNow);

        UpdateCore(name, packageCategoryId, description, start, expire, isQuantityAllowed, fullPeriod, postPaid);
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
        bool isQuantityAllowed,
        bool fullPeriod,
        bool postPaid)
    {
        Name = Guard.RequiredMaxLength(name, nameof(Name), 255);
        PackageCategoryId = Guard.PositiveId(packageCategoryId, nameof(PackageCategoryId));
        if (!Enum.IsDefined((PackageCategoryType)PackageCategoryId))
        {
            throw new DomainException($"Package category {PackageCategoryId} is not supported.");
        }

        Description = Guard.OptionalMaxLength(description, nameof(Description), 2000);

        if (start.HasValue && expire.HasValue && start.Value > expire.Value)
        {
            throw new DomainException("Package start date cannot be later than expire date.");
        }

        Start = start;
        Expire = expire;
        IsQuantityAllowed = isQuantityAllowed;
        FullPeriod = fullPeriod;
        PostPaid = postPaid;
    }

    private void ReplaceFrequencies(IEnumerable<PackageFrequencyDefinition> frequencies, DateTime utcNow)
    {
        var requested = frequencies.ToList();
        EnsureUniqueFrequencyNames(requested);

        var retainedFrequencies = new HashSet<PackageFrequency>();

        foreach (var requestedFrequency in requested)
        {
            PackageFrequency? existing;
            if (requestedFrequency.Id > 0)
            {
                existing = _frequencies.FirstOrDefault(frequency => frequency.Id == requestedFrequency.Id);
                if (existing is null)
                {
                    throw new DomainException($"Package frequency {requestedFrequency.Id} does not belong to this package.");
                }
            }
            else
            {
                existing = _frequencies.FirstOrDefault(frequency =>
                    FrequencyNamesEqual(frequency.Name, requestedFrequency.Name));

                if (existing is null)
                {
                    existing = PackageFrequency.Create(
                        requestedFrequency.Name,
                        requestedFrequency.Frequency,
                        requestedFrequency.IsActive,
                        utcNow);

                    _frequencies.Add(existing);
                }
            }

            existing.Update(requestedFrequency.Name, requestedFrequency.Frequency, requestedFrequency.IsActive);
            retainedFrequencies.Add(existing);
        }

        _frequencies.RemoveAll(frequency => !retainedFrequencies.Contains(frequency));
    }

    private void ReplaceServices(IEnumerable<PackageServiceDefinition> services)
    {
        var requested = services.ToList();
        EnsureUniqueServices(requested);

        var retainedServices = new HashSet<PackageService>();

        foreach (var requestedService in requested)
        {
            PackageService? existing;
            if (requestedService.Id > 0)
            {
                existing = _services.FirstOrDefault(service => service.Id == requestedService.Id);
                if (existing is null)
                {
                    throw new DomainException($"Package service {requestedService.Id} does not belong to this package.");
                }
            }
            else
            {
                existing = _services.FirstOrDefault(service => service.ServiceId == requestedService.ServiceId);

                if (existing is null)
                {
                    existing = PackageService.Create(
                        requestedService.ServiceId,
                        requestedService.DefaultInstances,
                        requestedService.MinimumInstances,
                        requestedService.MaximumInstances);

                    _services.Add(existing);
                }
            }

            existing.Update(
                requestedService.ServiceId,
                requestedService.DefaultInstances,
                requestedService.MinimumInstances,
                requestedService.MaximumInstances);
            retainedServices.Add(existing);
        }

        _services.RemoveAll(service => !retainedServices.Contains(service));
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
            .GroupBy(frequency => NormalizeFrequencyName(frequency.Name), StringComparer.OrdinalIgnoreCase)
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

    private static bool FrequencyNamesEqual(string name, string requestedName)
    {
        return string.Equals(
            NormalizeFrequencyName(name),
            NormalizeFrequencyName(requestedName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFrequencyName(string? name)
    {
        return name?.Trim() ?? string.Empty;
    }
}
