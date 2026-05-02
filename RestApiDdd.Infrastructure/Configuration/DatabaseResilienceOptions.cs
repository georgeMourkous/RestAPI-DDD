namespace RestApiDdd.Infrastructure.Configuration;

public sealed class DatabaseResilienceOptions
{
    public const string SectionName = "Database";

    public int ConnectionTimeoutSeconds { get; init; } = 15;

    public int CommandTimeoutSeconds { get; init; } = 30;

    public int MaxRetryCount { get; init; } = 3;

    public int MaxRetryDelaySeconds { get; init; } = 5;

    public bool Pooling { get; init; } = true;

    public int MinPoolSize { get; init; } = 0;

    public int MaxPoolSize { get; init; } = 100;

    public int DbContextPoolSize { get; init; } = 128;
}
