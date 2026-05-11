using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Domain.Tests;

internal static class DomainTestData
{
    public static readonly DateTime UtcNow = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);

    public static PackageFrequencyDefinition FrequencyDefinition(
        int id = 0,
        string name = "Monthly",
        int frequency = 30,
        bool isActive = true)
    {
        return new PackageFrequencyDefinition(id, name, frequency, isActive);
    }

    public static PackageServiceDefinition ServiceDefinition(
        int id = 0,
        int serviceId = 101,
        int defaultInstances = 1,
        int minimumInstances = 0,
        int? maximumInstances = 3)
    {
        return new PackageServiceDefinition(id, serviceId, defaultInstances, minimumInstances, maximumInstances);
    }

    public static Package CreatePackage(
        DateTime? start = null,
        DateTime? expire = null,
        IEnumerable<PackageFrequencyDefinition>? frequencies = null,
        IEnumerable<PackageServiceDefinition>? services = null,
        DateTime? utcNow = null,
        bool fullPeriod = false,
        bool postPaid = false)
    {
        return Package.Create(
            "Starter",
            packageCategoryId: 1,
            description: "Base package",
            start,
            expire,
            isQuantityAllowed: true,
            frequencies ?? [FrequencyDefinition()],
            services ?? [ServiceDefinition()],
            utcNow ?? UtcNow,
            fullPeriod,
            postPaid);
    }

    public static void SetEntityId(Entity entity, int id)
    {
        var setter = typeof(Entity).GetProperty(nameof(Entity.Id))!.GetSetMethod(nonPublic: true)!;
        setter.Invoke(entity, [id]);
    }
}
