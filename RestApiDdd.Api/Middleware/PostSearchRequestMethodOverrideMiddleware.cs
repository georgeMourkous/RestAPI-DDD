namespace RestApiDdd.Api.Middleware;

public sealed class PostSearchRequestMethodOverrideMiddleware(RequestDelegate next)
{
    private const string SearchRouteSuffix = "/Search";

    public Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) && EndsWithSearchRoute(context.Request.Path))
        {
            context.Request.Method = HttpMethods.Get;
        }

        return next(context);
    }

    private static bool EndsWithSearchRoute(PathString path)
    {
        var value = path.Value?.TrimEnd('/');

        return value?.EndsWith(SearchRouteSuffix, StringComparison.OrdinalIgnoreCase) == true;
    }
}
