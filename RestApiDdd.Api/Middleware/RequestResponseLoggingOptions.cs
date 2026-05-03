namespace RestApiDdd.Api.Middleware;

public sealed class RequestResponseLoggingOptions
{
    public const string SectionName = "RequestResponseLogging";

    public bool Enabled { get; init; } = true;

    public int MaxBodyLength { get; init; } = 16_384;
}
