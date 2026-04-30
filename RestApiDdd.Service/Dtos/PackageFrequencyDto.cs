namespace RestApiDdd.Service.Dtos;

public sealed class PackageFrequencyDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Frequency { get; init; }

    public bool IsActive { get; init; }

    public DateTime? Created { get; init; }
}
