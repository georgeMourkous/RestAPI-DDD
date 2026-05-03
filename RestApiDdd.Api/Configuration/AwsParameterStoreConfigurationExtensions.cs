using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace RestApiDdd.Api.Configuration;

public static class AwsParameterStoreConfigurationExtensions
{
    public static async Task LoadAwsParameterStoreSecretsAsync(this ConfigurationManager configuration, CancellationToken cancellationToken = default)
    {
        var options = configuration
            .GetSection(AwsParameterStoreOptions.SectionName)
            .Get<AwsParameterStoreOptions>() ?? new AwsParameterStoreOptions();

        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            throw new InvalidOperationException("AwsParameterStore:Region is required when AwsParameterStore is enabled.");
        }

        var parameterMappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Jwt:SigningKey"] = options.JwtSigningKeyParameterName ?? string.Empty,
            ["ConnectionStrings:DefaultConnection"] = options.DefaultConnectionParameterName ?? string.Empty
        };

        var requestedParameterNames = parameterMappings.Values
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (requestedParameterNames.Length != parameterMappings.Count)
        {
            throw new InvalidOperationException(
                "AwsParameterStore parameter names must be configured for Jwt:SigningKey and ConnectionStrings:DefaultConnection when AwsParameterStore is enabled.");
        }

        var region = RegionEndpoint.GetBySystemName(options.Region);
        using var client = new AmazonSimpleSystemsManagementClient(region);

        var response = await client.GetParametersAsync(
            new GetParametersRequest
            {
                Names = requestedParameterNames.ToList(),
                WithDecryption = true
            },
            cancellationToken);

        if (response.InvalidParameters.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unable to load AWS Parameter Store values. Invalid parameters: {string.Join(", ", response.InvalidParameters)}");
        }

        var valuesByName = response.Parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal);
        Dictionary<string, string?> loadedValues = parameterMappings.ToDictionary(
            mapping => mapping.Key,
            mapping => valuesByName.TryGetValue(mapping.Value, out var value)
                ? (string?)value
                : throw new InvalidOperationException($"AWS Parameter Store value '{mapping.Value}' was not returned."));

        configuration.AddInMemoryCollection(loadedValues);
    }
}
