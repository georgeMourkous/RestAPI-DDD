using Microsoft.Data.SqlClient;

namespace RestApiDdd.Infrastructure.Configuration;

internal static class SqlServerConnectionStringFactory
{
    public static string Build(string baseConnectionString, DatabaseResilienceOptions options)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            ConnectTimeout = options.ConnectionTimeoutSeconds,
            Pooling = options.Pooling,
            MinPoolSize = options.MinPoolSize,
            MaxPoolSize = options.MaxPoolSize
        };

        return builder.ConnectionString;
    }
}
