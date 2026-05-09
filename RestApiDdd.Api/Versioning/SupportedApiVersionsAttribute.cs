using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using RestApiDdd.Api.Controllers;

namespace RestApiDdd.Api.Versioning;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SupportedApiVersionsAttribute : Attribute, IAsyncActionFilter
{
    private const string VersionRouteValueName = "version";

    public SupportedApiVersionsAttribute(ApiVersion fromVersion)
    {
        FromVersion = fromVersion;
    }

    public SupportedApiVersionsAttribute(ApiVersion fromVersion, ApiVersion toVersion)
        : this(fromVersion)
    {
        if (fromVersion > toVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(toVersion), "ToVersion must be greater than or equal to FromVersion.");
        }

        ToVersion = toVersion;
    }

    public ApiVersion FromVersion { get; }

    public ApiVersion? ToVersion { get; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!TryGetRequestedVersion(context, out var requestedVersion)
            || !IsRequestedVersionSupported(context, requestedVersion))
        {
            context.Result = new NotFoundResult();
            return;
        }

        if (context.Controller is ApiControllerBase controller)
        {
            controller.SetRequestedApiVersion(requestedVersion);
        }

        await next();
    }

    private static bool TryGetRequestedVersion(ActionExecutingContext context, out ApiVersion requestedVersion)
    {
        requestedVersion = default;

        if (!context.RouteData.Values.TryGetValue(VersionRouteValueName, out var rawVersion)
            || rawVersion is null)
        {
            return false;
        }

        return Enum.TryParse(rawVersion.ToString(), ignoreCase: true, out requestedVersion)
            && Enum.IsDefined(requestedVersion);
    }

    private static bool IsRequestedVersionSupported(ActionExecutingContext context, ApiVersion requestedVersion)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            return true;
        }

        var controllerVersion = controllerActionDescriptor.ControllerTypeInfo
            .GetCustomAttributes(typeof(SupportedApiVersionsAttribute), inherit: true)
            .OfType<SupportedApiVersionsAttribute>()
            .LastOrDefault();
        var actionVersion = controllerActionDescriptor.MethodInfo
            .GetCustomAttributes(typeof(SupportedApiVersionsAttribute), inherit: true)
            .OfType<SupportedApiVersionsAttribute>()
            .LastOrDefault();

        return Supports(controllerVersion, requestedVersion)
            && Supports(actionVersion, requestedVersion);
    }

    private static bool Supports(SupportedApiVersionsAttribute? attribute, ApiVersion requestedVersion)
    {
        return attribute is null
            || requestedVersion >= attribute.FromVersion
            && (attribute.ToVersion is null || requestedVersion <= attribute.ToVersion.Value);
    }
}
