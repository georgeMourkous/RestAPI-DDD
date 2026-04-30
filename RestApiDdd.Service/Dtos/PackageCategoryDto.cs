namespace RestApiDdd.Service.Dtos;

public sealed class PackageCategoryDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool Visible { get; init; }
}
