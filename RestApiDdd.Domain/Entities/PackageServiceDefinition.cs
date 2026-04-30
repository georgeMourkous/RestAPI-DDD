namespace RestApiDdd.Domain.Entities;

public sealed record PackageServiceDefinition(
    int Id,
    int ServiceId,
    int DefaultInstances,
    int MinimumInstances,
    int? MaximumInstances);
