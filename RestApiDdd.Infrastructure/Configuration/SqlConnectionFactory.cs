using Microsoft.Data.SqlClient;
using RestApiDdd.Infrastructure.Resilience;

namespace RestApiDdd.Infrastructure.Configuration;

internal interface ISqlConnectionFactory
{
    Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}

internal sealed class SqlConnectionFactory(string connectionString, DatabaseResilienceOptions options) : ISqlConnectionFactory
{
    private readonly int _maxRetryCount = Math.Max(0, options.MaxRetryCount);
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(Math.Max(0, options.MaxRetryDelaySeconds));

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var failedAttempts = 0;

        while (true)
        {
            var connection = new SqlConnection(connectionString);

            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (Exception exception) when (ShouldRetry(exception, failedAttempts))
            {
                await connection.DisposeAsync();
                failedAttempts++;

                if (_retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }

    private bool ShouldRetry(Exception exception, int failedAttempts)
    {
        return failedAttempts < _maxRetryCount
            && DatabaseExceptionClassifier.IsConnectivityFailure(exception);
    }
}
