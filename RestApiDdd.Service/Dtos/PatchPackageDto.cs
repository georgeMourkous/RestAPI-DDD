namespace RestApiDdd.Service.Dtos;

public sealed class PatchPackageDto
{
    public string? Name { get; init; }

    public int? PackageCategoryId { get; init; }

    public string? Description { get; init; }

    public bool ClearDescription { get; init; }

    public DateTime? Start { get; init; }

    public bool ClearStart { get; init; }

    public DateTime? Expire { get; init; }

    public bool ClearExpire { get; init; }

    public bool? IsQuantityAllowed { get; init; }

    public List<PackageFrequencyDto>? Frequencies { get; init; }

    public List<PackageServiceDto>? Services { get; init; }
}
