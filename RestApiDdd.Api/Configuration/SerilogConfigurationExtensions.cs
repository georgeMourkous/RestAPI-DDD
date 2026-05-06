using AWS.Logger;
using AWS.Logger.SeriLog;
using RestApiDdd.Api.Middleware;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;

namespace RestApiDdd.Api.Configuration;

public static class SerilogConfigurationExtensions
{
    public static IHostBuilder UseConfiguredSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            if (context.HostingEnvironment.IsProduction())
            {
                ConfigureProductionSerilog(context.Configuration, loggerConfiguration);
            }
            else
            {
                loggerConfiguration.ReadFrom.Configuration(context.Configuration);
            }

            loggerConfiguration
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });
    }

    private static void ConfigureProductionSerilog(IConfiguration configuration, LoggerConfiguration loggerConfiguration)
    {
        var cloudWatchOptions = configuration
            .GetSection(ProductionCloudWatchLogOptions.SectionName)
            .Get<ProductionCloudWatchLogOptions>() ?? new ProductionCloudWatchLogOptions();

        var cloudWatchFormatter = new JsonFormatter(renderMessage: true);

        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "RestApiDdd.Api")
            .WriteTo.Logger(apiLogger => apiLogger
                .Filter.ByExcluding(Matching.FromSource<RequestResponseLoggingMiddleware>())
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .WriteTo.AWSSeriLog(CreateAwsLoggerConfig(
                    cloudWatchOptions.Region,
                    cloudWatchOptions.ApiLogGroup,
                    cloudWatchOptions.ApiLogStreamNamePrefix,
                    cloudWatchOptions.DisableLogGroupCreation,
                    cloudWatchOptions.NewLogGroupRetentionInDays),
                    textFormatter: cloudWatchFormatter))
            .WriteTo.Logger(requestLogger => requestLogger
                .Filter.ByIncludingOnly(Matching.FromSource<RequestResponseLoggingMiddleware>())
                .WriteTo.AWSSeriLog(CreateAwsLoggerConfig(
                    cloudWatchOptions.Region,
                    cloudWatchOptions.RequestLogGroup,
                    cloudWatchOptions.RequestLogStreamNamePrefix,
                    cloudWatchOptions.DisableLogGroupCreation,
                    cloudWatchOptions.NewLogGroupRetentionInDays),
                    textFormatter: cloudWatchFormatter));
    }

    private static AWSLoggerConfig CreateAwsLoggerConfig(
        string region,
        string logGroup,
        string logStreamNamePrefix,
        bool disableLogGroupCreation,
        int? newLogGroupRetentionInDays)
    {
        return new AWSLoggerConfig
        {
            Region = region,
            LogGroup = logGroup,
            LogStreamNamePrefix = logStreamNamePrefix,
            DisableLogGroupCreation = disableLogGroupCreation,
            NewLogGroupRetentionInDays = newLogGroupRetentionInDays
        };
    }
}
