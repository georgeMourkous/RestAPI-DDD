using System.Data;
using Dapper;
using RestApiDdd.Infrastructure.Configuration;
using RestApiDdd.Infrastructure.Resilience;

namespace RestApiDdd.Infrastructure.Repositories;

internal abstract class DapperRepository(
    ISqlConnectionFactory connectionFactory,
    IDatabaseResilienceExecutor resilienceExecutor,
    DatabaseResilienceOptions databaseOptions)
{
    protected Task<IReadOnlyList<T>> QueryStoredProcedureAsync<T>(
        string storedProcedureName,
        object? parameters,
        CancellationToken cancellationToken = default)
    {
        return resilienceExecutor.ExecuteAsync<IReadOnlyList<T>>(
            async token =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(token);
                var command = new CommandDefinition(
                    storedProcedureName,
                    parameters,
                    commandTimeout: databaseOptions.CommandTimeoutSeconds,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: token);
                var rows = await connection.QueryAsync<T>(command);

                return rows.ToArray();
            },
            cancellationToken);
    }
}
