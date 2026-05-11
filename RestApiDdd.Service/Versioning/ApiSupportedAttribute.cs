namespace RestApiDdd.Service.Versioning;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ApiSupportedAttribute : Attribute
{
    public ApiSupportedAttribute(ApiVersion fromVersion)
    {
        FromVersion = fromVersion;
    }

    public ApiSupportedAttribute(ApiVersion fromVersion, ApiVersion toVersion)
        : this(fromVersion)
    {
        if (fromVersion > toVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(toVersion), "ToVersion must be greater than or equal to FromVersion.");
        }

        ToVersion = toVersion;
    }

    public ApiVersion FromVersion { get; }

    public ApiVersion? ToVersion { get; }

    public bool Supports(ApiVersion requestedVersion)
    {
        return requestedVersion >= FromVersion
            && (ToVersion is null || requestedVersion <= ToVersion.Value);
    }
}
