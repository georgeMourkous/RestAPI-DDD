using Microsoft.Data.SqlClient;

namespace RestApiDdd.Infrastructure.Resilience;

internal static class DatabaseExceptionClassifier
{
    private static readonly HashSet<int> ConnectivitySqlErrorNumbers =
    [
        -2,
        2,
        20,
        53,
        64,
        233,
        4060,
        10053,
        10054,
        10060,
        10928,
        10929,
        11001,
        40197,
        40501,
        40613
    ];

    public static bool IsConnectivityFailure(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            SqlException sqlException => sqlException.Errors.Cast<SqlError>()
                .Any(error => ConnectivitySqlErrorNumbers.Contains(error.Number)),
            _ when exception.InnerException is not null => IsConnectivityFailure(exception.InnerException),
            _ => false
        };
    }
}
