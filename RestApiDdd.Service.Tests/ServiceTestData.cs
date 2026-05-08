using System.Reflection;
using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;
using RestApiDdd.Service.Dtos;
using ServiceEntity = RestApiDdd.Domain.Entities.Service;

namespace RestApiDdd.Service.Tests;

internal static class ServiceTestData
{
    public static readonly DateTime UtcNow = new(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc);

    public static CreatePackageDto CreatePackageDto(
        string name = "Starter",
        int packageCategoryId = 1,
        string? description = "Base package",
        DateTime? start = null,
        DateTime? expire = null,
        bool isQuantityAllowed = true,
        List<PackageFrequencyDto>? frequencies = null,
        List<PackageServiceDto>? services = null)
    {
        return new CreatePackageDto
        {
            Name = name,
            PackageCategoryId = packageCategoryId,
            Description = description,
            Start = start,
            Expire = expire,
            IsQuantityAllowed = isQuantityAllowed,
            Frequencies = frequencies ?? [FrequencyDto()],
            Services = services ?? [ServiceDto()]
        };
    }

    public static UpdatePackageDto UpdatePackageDto(
        string name = "Starter",
        int packageCategoryId = 1,
        string? description = "Base package",
        DateTime? start = null,
        DateTime? expire = null,
        bool isQuantityAllowed = true,
        List<PackageFrequencyDto>? frequencies = null,
        List<PackageServiceDto>? services = null)
    {
        return new UpdatePackageDto
        {
            Name = name,
            PackageCategoryId = packageCategoryId,
            Description = description,
            Start = start,
            Expire = expire,
            IsQuantityAllowed = isQuantityAllowed,
            Frequencies = frequencies ?? [FrequencyDto()],
            Services = services ?? [ServiceDto()]
        };
    }

    public static PackageFrequencyDto FrequencyDto(
        int id = 0,
        string name = "Monthly",
        int frequency = 30,
        bool isActive = true,
        DateTime? created = null)
    {
        return new PackageFrequencyDto
        {
            Id = id,
            Name = name,
            Frequency = frequency,
            IsActive = isActive,
            Created = created
        };
    }

    public static PackageServiceDto ServiceDto(
        int id = 0,
        int serviceId = 101,
        int defaultInstances = 1,
        int minimumInstances = 0,
        int? maximumInstances = 3)
    {
        return new PackageServiceDto
        {
            Id = id,
            ServiceId = serviceId,
            DefaultInstances = defaultInstances,
            MinimumInstances = minimumInstances,
            MaximumInstances = maximumInstances
        };
    }

    public static Package Package(
        int id = 1,
        string name = "Starter",
        int packageCategoryId = 1,
        string? description = "Base package",
        DateTime? start = null,
        DateTime? expire = null,
        bool isQuantityAllowed = true,
        DateTime? utcNow = null,
        IEnumerable<PackageFrequencyDefinition>? frequencies = null,
        IEnumerable<PackageServiceDefinition>? services = null)
    {
        var package = RestApiDdd.Domain.Entities.Package.Create(
            name,
            packageCategoryId,
            description,
            start,
            expire,
            isQuantityAllowed,
            frequencies ?? [new PackageFrequencyDefinition(0, "Monthly", 30, true)],
            services ?? [new PackageServiceDefinition(0, 101, 1, 0, 3)],
            utcNow ?? UtcNow);

        SetEntityId(package, id);
        return package;
    }

    public static PackageCategory Category(int id = 1, string name = "Default")
    {
        var category = new PackageCategory(name, sortOrder: 0, visible: true);
        SetEntityId(category, id);
        return category;
    }

    public static ServiceEntity Service(int id = 101, string name = "Consultation")
    {
        var service = new ServiceEntity(name, description: null);
        SetEntityId(service, id);
        return service;
    }

    public static void SetEntityId(Entity entity, int id)
    {
        var setter = typeof(Entity)
            .GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public)!
            .GetSetMethod(nonPublic: true)!;

        setter.Invoke(entity, [id]);
    }
}
