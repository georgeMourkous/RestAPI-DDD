namespace RestApiDdd.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "RestApiDdd";

    public string Audience { get; init; } = "RestApiDdd.Clients";

    public string SigningKey { get; init; } = string.Empty;
}
