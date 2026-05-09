using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RestApiDdd.Api.Controllers;
using RestApiDdd.Api.Versioning;

namespace RestApiDdd.Api.Tests.Versioning;

public sealed class SupportedApiVersionsAttributeTests
{
    [Fact]
    public async Task OnActionExecutionAsync_CallsNext_WhenControllerVersionSupportsRequestedVersion()
    {
        var result = await ExecuteAsync("v1", typeof(V1OnlyController), nameof(V1OnlyController.InheritedAction));

        Assert.True(result.NextCalled);
        Assert.Null(result.Context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_CallsNext_WhenControllerRangeHasNoToVersionAndRequestedVersionIsAfterFromVersion()
    {
        var result = await ExecuteAsync("v2", typeof(V1AndLaterController), nameof(V1AndLaterController.InheritedAction));

        Assert.True(result.NextCalled);
        Assert.Null(result.Context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ReturnsNotFound_WhenRequestedVersionIsOutsideBoundedControllerRange()
    {
        var result = await ExecuteAsync("v2", typeof(V1OnlyController), nameof(V1OnlyController.InheritedAction));

        Assert.False(result.NextCalled);
        Assert.IsType<NotFoundResult>(result.Context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ReturnsNotFound_WhenRequestedVersionIsInvalid()
    {
        var result = await ExecuteAsync("beta", typeof(V1OnlyController), nameof(V1OnlyController.InheritedAction));

        Assert.False(result.NextCalled);
        Assert.IsType<NotFoundResult>(result.Context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_UsesActionRangeToNarrowControllerRange()
    {
        var v1Result = await ExecuteAsync("v1", typeof(V1ToV2Controller), nameof(V1ToV2Controller.V2OnlyAction));
        var v2Result = await ExecuteAsync("v2", typeof(V1ToV2Controller), nameof(V1ToV2Controller.V2OnlyAction));

        Assert.False(v1Result.NextCalled);
        Assert.IsType<NotFoundResult>(v1Result.Context.Result);
        Assert.True(v2Result.NextCalled);
        Assert.Null(v2Result.Context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_SetsRequestedApiVersionOnApiControllerBase_WhenVersionIsSupported()
    {
        var controller = new V1AndLaterController();

        var result = await ExecuteAsync(
            "v2",
            typeof(V1AndLaterController),
            nameof(V1AndLaterController.InheritedAction),
            controller);

        Assert.True(result.NextCalled);
        Assert.Equal(ApiVersion.v2, controller.ApiVersionForTest);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ReturnsNotFound_WhenActionRangeAllowsVersionButControllerRangeDoesNot()
    {
        var result = await ExecuteAsync("v2", typeof(V1OnlyController), nameof(V1OnlyController.V2OnlyAction));

        Assert.False(result.NextCalled);
        Assert.IsType<NotFoundResult>(result.Context.Result);
    }

    [Fact]
    public void Constructor_Throws_WhenRangeIsInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SupportedApiVersionsAttribute(ApiVersion.v2, ApiVersion.v1));
    }

    private static async Task<FilterResult> ExecuteAsync(
        string version,
        Type controllerType,
        string actionName,
        object? controller = null)
    {
        var context = CreateActionExecutingContext(version, controllerType, actionName, controller);
        var filter = new SupportedApiVersionsAttribute(ApiVersion.v1, ApiVersion.v2);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            context,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], controller ?? new object()));
            });

        return new FilterResult(context, nextCalled);
    }

    private static ActionExecutingContext CreateActionExecutingContext(
        string version,
        Type controllerType,
        string actionName,
        object? controller)
    {
        var methodInfo = controllerType.GetMethod(
            actionName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Action {actionName} was not found.");

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = methodInfo
        };
        var routeData = new RouteData();
        routeData.Values["version"] = version;
        var actionContext = new ActionContext(new DefaultHttpContext(), routeData, actionDescriptor);

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller ?? new object());
    }

    private sealed record FilterResult(ActionExecutingContext Context, bool NextCalled);

    [SupportedApiVersions(ApiVersion.v1, ApiVersion.v1)]
    private sealed class V1OnlyController
    {
        public void InheritedAction()
        {
        }

        [SupportedApiVersions(ApiVersion.v2, ApiVersion.v2)]
        public void V2OnlyAction()
        {
        }
    }

    [SupportedApiVersions(ApiVersion.v1)]
    private sealed class V1AndLaterController : ApiControllerBase
    {
        public ApiVersion? ApiVersionForTest => RequestedApiVersion;

        public void InheritedAction()
        {
        }
    }

    [SupportedApiVersions(ApiVersion.v1, ApiVersion.v2)]
    private sealed class V1ToV2Controller
    {
        [SupportedApiVersions(ApiVersion.v2, ApiVersion.v2)]
        public void V2OnlyAction()
        {
        }
    }
}
