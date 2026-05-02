using RestApiDdd.Service.Exceptions;

namespace RestApiDdd.Infrastructure.Resilience;

internal sealed class DatabaseResilienceExecutor : IDatabaseResilienceExecutor
{
    private const string DefaultMessage = "The database is currently unavailable. Please try again later.";

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        try
        {
            await operation(cancellationToken);
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsConnectivityFailure(exception))
        {
            throw new DatabaseConnectionException(DefaultMessage, exception);
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsConnectivityFailure(exception))
        {
            throw new DatabaseConnectionException(DefaultMessage, exception);
        }
    }
}
