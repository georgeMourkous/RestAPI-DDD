using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Service.Abstractions;

public interface ISensorReadingRepository
{
    Task<IReadOnlyList<SensorReadingDto>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}
