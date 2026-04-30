namespace RestApiDdd.Service.Dtos;

public sealed class ServiceDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
