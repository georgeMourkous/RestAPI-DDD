namespace RestApiDdd.Api.Configuration;

public sealed class ProductionCloudWatchLogOptions
{
    public const string SectionName = "ProductionCloudWatchLogs";

    public string Region { get; init; } = "us-east-1";

    public string ApiLogGroup { get; init; } = "/rest-api-ddd/api";

    public string ApiLogStreamNamePrefix { get; init; } = "api-";

    public string RequestLogGroup { get; init; } = "/rest-api-ddd/api-requests";

    public string RequestLogStreamNamePrefix { get; init; } = "requests-";

    public bool DisableLogGroupCreation { get; init; }

    public int? NewLogGroupRetentionInDays { get; init; } = 30;
}
