using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Exceptions;

namespace RestApiDdd.Service.SensorReadings;

internal sealed class GetSensorReadingsQueryHandler(
    ISensorReadingRepository sensorReadingRepository) : IQueryHandler<GetSensorReadingsQuery, IReadOnlyList<SensorReadingDto>>
{
    private const int DefaultPageSize = 10;
    private const int DefaultPageNumber = 1;

    public Task<IReadOnlyList<SensorReadingDto>> HandleAsync(
        GetSensorReadingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var request = Normalize(query.Request);
        Validate(request);

        return sensorReadingRepository.SearchAsync(request, cancellationToken);
    }

    private static SearchRequest Normalize(SearchRequest request)
    {
        return new SearchRequest
        {
            StudyId = NormalizeText(request.StudyId),
            SiteId = NormalizeText(request.SiteId),
            SubjectId = NormalizeText(request.SubjectId),
            DeviceId = NormalizeText(request.DeviceId),
            PageSize = request.PageSize ?? DefaultPageSize,
            PageNumber = request.PageNumber ?? DefaultPageNumber
        };
    }

    private static void Validate(SearchRequest request)
    {
        var errors = new List<string>();

        if (request.PageSize is < 1)
        {
            errors.Add("PageSize must be greater than zero.");
        }

        if (request.PageNumber is < 1)
        {
            errors.Add("PageNumber must be greater than zero.");
        }

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
