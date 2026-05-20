using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestApiDdd.Api.Versioning;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.SensorReadings;
using RestApiDdd.Service.Versioning;

namespace RestApiDdd.Api.Controllers;

[ApiController]
[Route("api/{version}/SensorReading")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[SupportedApiVersions(ApiVersion.v2)]
public sealed class SensorReadingController(
    IQueryHandler<GetSensorReadingsQuery, IReadOnlyList<SensorReadingDto>> getSensorReadingsQueryHandler) : ApiControllerBase
{
    [HttpGet("Search")]
    [SupportedApiVersions(ApiVersion.v2)]
    [ProducesResponseType(typeof(IReadOnlyList<SensorReadingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SensorReadingDto>>> GetSensorReadings(
        SearchRequest? request,
        CancellationToken cancellationToken)
    {
        var readings = await getSensorReadingsQueryHandler.HandleAsync(
            new GetSensorReadingsQuery(request ?? new SearchRequest()),
            cancellationToken);

        return Ok(readings);
    }
}
