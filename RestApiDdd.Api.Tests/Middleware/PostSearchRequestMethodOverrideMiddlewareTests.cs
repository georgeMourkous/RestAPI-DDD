using Microsoft.AspNetCore.Http;
using RestApiDdd.Api.Middleware;

namespace RestApiDdd.Api.Tests.Middleware;

public sealed class PostSearchRequestMethodOverrideMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ChangesPostToGet_WhenPathEndsWithSearch()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v2/SensorReading/Search";
        string? methodSeenByNext = null;
        var middleware = new PostSearchRequestMethodOverrideMiddleware(httpContext =>
        {
            methodSeenByNext = httpContext.Request.Method;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(HttpMethods.Get, context.Request.Method);
        Assert.Equal(HttpMethods.Get, methodSeenByNext);
    }

    [Fact]
    public async Task InvokeAsync_ChangesPostToGet_WhenSearchPathHasTrailingSlash()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v2/SensorReading/Search/";
        var middleware = new PostSearchRequestMethodOverrideMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(HttpMethods.Get, context.Request.Method);
    }

    [Fact]
    public async Task InvokeAsync_KeepsPost_WhenPathDoesNotEndWithSearch()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v2/SensorReading";
        var middleware = new PostSearchRequestMethodOverrideMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(HttpMethods.Post, context.Request.Method);
    }

    [Fact]
    public async Task InvokeAsync_KeepsGet_WhenPathEndsWithSearch()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v2/SensorReading/Search";
        var middleware = new PostSearchRequestMethodOverrideMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(HttpMethods.Get, context.Request.Method);
    }
}
