namespace RestApiDdd.Service.Dtos;

public sealed class SensorReadingDto
{
    public long Id { get; init; }

    public Guid MessageId { get; init; }

    public string StudyId { get; init; } = string.Empty;

    public string? SiteId { get; init; }

    public string SubjectId { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string MeasurementType { get; init; } = string.Empty;

    public DateTime MeasurementTimestampUtc { get; init; }

    public DateTime ReceivedTimestampUtc { get; init; }

    public decimal? NumericValue1 { get; init; }

    public decimal? NumericValue2 { get; init; }

    public string? Unit { get; init; }

    public string? PayloadJson { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
