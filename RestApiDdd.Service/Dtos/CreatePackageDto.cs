namespace RestApiDdd.Service.Dtos;

public sealed class CreatePackageDto
{
    public string Name { get; init; } = string.Empty;

    public int PackageCategoryId { get; init; }

    public string? Description { get; init; }

    public DateTime? Start { get; init; }

    public DateTime? Expire { get; init; }

    public bool IsQuantityAllowed { get; init; }

    public List<PackageFrequencyDto> Frequencies { get; init; } = [];

    public List<PackageServiceDto> Services { get; init; } = [];
}
