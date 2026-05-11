using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using RestApiDdd.Api.Middleware;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Api.Tests.Versioning;

public sealed class ApiVersionedDtoMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsBadRequest_WhenRequestIncludesUnsupportedProperty()
    {
        var context = CreateContext("v1", nameof(TestController.CreatePackage));
        context.Request.ContentType = "application/json";
        await WriteRequestBodyAsync(context, """{"name":"Starter","packageCategoryId":1,"fullPeriod":true}""");

        var middleware = new ApiVersionedDtoMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("fullPeriod", body);
    }

    [Fact]
    public async Task InvokeAsync_AllowsRequest_WhenPropertyIsSupportedByVersion()
    {
        var context = CreateContext("v2", nameof(TestController.CreatePackage));
        context.Request.ContentType = "application/json";
        await WriteRequestBodyAsync(context, """{"name":"Starter","packageCategoryId":1,"fullPeriod":true}""");
        var nextCalled = false;

        var middleware = new ApiVersionedDtoMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_RemovesUnsupportedPropertiesFromJsonResponse()
    {
        var context = CreateContext("v1", nameof(TestController.GetPackage));
        var middleware = new ApiVersionedDtoMiddleware(async httpContext =>
        {
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(
                """{"id":5,"name":"Starter","packageCategoryId":1,"fullPeriod":true,"postPaid":true}""");
        });

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.DoesNotContain("fullPeriod", body);
        Assert.DoesNotContain("postPaid", body);
        Assert.Contains("packageCategoryId", body);
    }

    private static DefaultHttpContext CreateContext(string version, string actionName)
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues = new RouteValueDictionary { ["version"] = version };
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new ProducesResponseTypeAttribute(typeof(PackageDto), StatusCodes.Status200OK),
                CreateActionDescriptor(actionName)),
            "test endpoint"));

        return context;
    }

    private static ControllerActionDescriptor CreateActionDescriptor(string actionName)
    {
        var methodInfo = typeof(TestController).GetMethod(actionName)
            ?? throw new InvalidOperationException($"Action {actionName} was not found.");

        return new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            MethodInfo = methodInfo,
            Parameters = methodInfo.GetParameters()
                .Select(parameterInfo => new ControllerParameterDescriptor
                {
                    Name = parameterInfo.Name!,
                    ParameterInfo = parameterInfo,
                    ParameterType = parameterInfo.ParameterType
                })
                .Cast<ParameterDescriptor>()
                .ToList()
        };
    }

    private static async Task WriteRequestBodyAsync(HttpContext context, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        await Task.CompletedTask;
    }

    private sealed class TestController
    {
        public void CreatePackage([FromBody] CreatePackageDto package)
        {
        }

        public PackageDto GetPackage()
        {
            return new PackageDto();
        }
    }
}
