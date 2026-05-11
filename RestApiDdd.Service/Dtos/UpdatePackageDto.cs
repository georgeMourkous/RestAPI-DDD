using RestApiDdd.Service.Versioning;

namespace RestApiDdd.Service.Dtos;

public sealed class UpdatePackageDto
{
    public string Name { get; init; } = string.Empty;

    public int PackageCategoryId { get; init; }

    public string? Description { get; init; }

    public DateTime? Start { get; init; }

    public DateTime? Expire { get; init; }

    public bool IsQuantityAllowed { get; init; }

    [ApiSupported(ApiVersion.v2)]
    public bool? FullPeriod { get; init; }

    [ApiSupported(ApiVersion.v2)]
    public bool? PostPaid { get; init; }

    public List<PackageFrequencyDto> Frequencies { get; init; } = [];

    public List<PackageServiceDto> Services { get; init; } = [];
}
