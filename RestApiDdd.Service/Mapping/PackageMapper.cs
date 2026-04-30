using RestApiDdd.Domain.Entities;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Mapping;

internal static class PackageMapper
{
    public static PackageDto ToDto(this Package package, DateTime utcNow)
    {
        return new PackageDto
        {
            Id = package.Id,
            Name = package.Name,
            PackageCategoryId = package.PackageCategoryId,
            Description = package.Description,
            Created = package.Created,
            Start = package.Start,
            Expire = package.Expire,
            IsQuantityAllowed = package.IsQuantityAllowed,
            IsActive = package.IsActiveAt(utcNow),
            Frequencies = package.Frequencies
                .OrderBy(frequency => frequency.Id)
                .Select(frequency => new PackageFrequencyDto
                {
                    Id = frequency.Id,
                    Name = frequency.Name,
                    Frequency = frequency.Frequency,
                    IsActive = frequency.IsActive,
                    Created = frequency.Created
                })
                .ToList(),
            Services = package.Services
                .OrderBy(service => service.Id)
                .Select(service => new PackageServiceDto
                {
                    Id = service.Id,
                    ServiceId = service.ServiceId,
                    DefaultInstances = service.DefaultInstances,
                    MinimumInstances = service.MinimumInstances,
                    MaximumInstances = service.MaximumInstances
                })
                .ToList()
        };
    }

    public static IReadOnlyList<PackageFrequencyDefinition> ToFrequencyDefinitions(this IEnumerable<PackageFrequencyDto>? frequencies)
    {
        return (frequencies ?? [])
            .Select(frequency => new PackageFrequencyDefinition(
                frequency.Id,
                frequency.Name,
                frequency.Frequency,
                frequency.IsActive))
            .ToArray();
    }

    public static IReadOnlyList<PackageServiceDefinition> ToServiceDefinitions(this IEnumerable<PackageServiceDto>? services)
    {
        return (services ?? [])
            .Select(service => new PackageServiceDefinition(
                service.Id,
                service.ServiceId,
                service.DefaultInstances,
                service.MinimumInstances,
                service.MaximumInstances))
            .ToArray();
    }
}
