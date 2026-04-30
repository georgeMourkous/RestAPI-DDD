using RestApiDdd.Domain.Common;

namespace RestApiDdd.Domain.Entities;

public sealed class PackageService : Entity
{
    private PackageService()
    {
    }

    private PackageService(int serviceId, int defaultInstances, int minimumInstances, int? maximumInstances)
    {
        Update(serviceId, defaultInstances, minimumInstances, maximumInstances);
    }

    public int PackageId { get; private set; }

    public Package? Package { get; private set; }

    public int ServiceId { get; private set; }

    public Service? Service { get; private set; }

    public int DefaultInstances { get; private set; }

    public int MinimumInstances { get; private set; }

    public int? MaximumInstances { get; private set; }

    internal static PackageService Create(int serviceId, int defaultInstances, int minimumInstances, int? maximumInstances)
    {
        return new PackageService(serviceId, defaultInstances, minimumInstances, maximumInstances);
    }

    internal void Update(int serviceId, int defaultInstances, int minimumInstances, int? maximumInstances)
    {
        ServiceId = Guard.PositiveId(serviceId, nameof(ServiceId));

        if (minimumInstances < 0)
        {
            throw new DomainException("Minimum instances cannot be negative.");
        }

        if (defaultInstances < minimumInstances)
        {
            throw new DomainException("Default instances cannot be less than minimum instances.");
        }

        if (maximumInstances.HasValue && maximumInstances.Value < defaultInstances)
        {
            throw new DomainException("Maximum instances cannot be less than default instances.");
        }

        DefaultInstances = defaultInstances;
        MinimumInstances = minimumInstances;
        MaximumInstances = maximumInstances;
    }
}
