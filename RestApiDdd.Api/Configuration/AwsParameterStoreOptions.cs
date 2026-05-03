namespace RestApiDdd.Api.Configuration;

public sealed class AwsParameterStoreOptions
{
    public const string SectionName = "AwsParameterStore";

    public bool Enabled { get; init; }

    public string? Region { get; init; }

    public string? JwtSigningKeyParameterName { get; init; }

    public string? DefaultConnectionParameterName { get; init; }
}
