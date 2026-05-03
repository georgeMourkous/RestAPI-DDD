namespace RestApiDdd.Api.Middleware;

public static class RequestResponseLoggingExtensions
{
    public static IServiceCollection AddRequestResponseLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RequestResponseLoggingOptions>(
            configuration.GetSection(RequestResponseLoggingOptions.SectionName));

        return services;
    }

    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
