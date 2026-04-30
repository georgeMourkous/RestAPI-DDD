namespace RestApiDdd.Service.Dtos;

public sealed class PackageServiceDto
{
    public int Id { get; init; }

    public int ServiceId { get; init; }

    public int DefaultInstances { get; init; }

    public int MinimumInstances { get; init; }

    public int? MaximumInstances { get; init; }
}
