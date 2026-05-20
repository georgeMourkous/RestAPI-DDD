namespace RestApiDdd.Service.Dtos;

public sealed class SearchRequest
{
    public string? StudyId { get; init; }

    public string? SiteId { get; init; }

    public string? SubjectId { get; init; }

    public string? DeviceId { get; init; }

    public int? PageSize { get; init; } = 10;

    public int? PageNumber { get; init; } = 1;
}
