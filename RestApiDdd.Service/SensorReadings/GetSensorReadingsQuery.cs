using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.SensorReadings;

public sealed record GetSensorReadingsQuery(SearchRequest Request) : IQuery<IReadOnlyList<SensorReadingDto>>;
