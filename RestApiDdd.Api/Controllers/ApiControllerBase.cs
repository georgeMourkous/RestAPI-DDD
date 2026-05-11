using Microsoft.AspNetCore.Mvc;
using RestApiDdd.Service.Versioning;

namespace RestApiDdd.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ApiVersion? RequestedApiVersion { get; private set; }

    internal void SetRequestedApiVersion(ApiVersion requestedApiVersion)
    {
        RequestedApiVersion = requestedApiVersion;
    }
}
