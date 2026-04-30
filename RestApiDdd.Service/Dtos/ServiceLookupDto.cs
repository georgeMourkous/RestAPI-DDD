namespace RestApiDdd.Service.Dtos;

public sealed class ServiceLookupDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
