using System.Data;
using Dapper;
using RestApiDdd.Infrastructure.Configuration;
using RestApiDdd.Infrastructure.Resilience;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Dtos;

namespace RestApiDdd.Infrastructure.Repositories;

internal sealed class SensorReadingRepository(
    ISqlConnectionFactory connectionFactory,
    IDatabaseResilienceExecutor resilienceExecutor,
    DatabaseResilienceOptions databaseOptions)
    : DapperRepository(connectionFactory, resilienceExecutor, databaseOptions), ISensorReadingRepository
{
    private const string StoredProcedureName = "dbo.GetSensorReading";

    public Task<IReadOnlyList<SensorReadingDto>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@StudyId", request.StudyId, DbType.String);
        parameters.Add("@SiteId", request.SiteId, DbType.String);
        parameters.Add("@SubjectId", request.SubjectId, DbType.String);
        parameters.Add("@DeviceId", request.DeviceId, DbType.String);
        parameters.Add("@PageSize", request.PageSize, DbType.Int32);
        parameters.Add("@PageNumber", request.PageNumber, DbType.Int32);

        return QueryStoredProcedureAsync<SensorReadingDto>(
            StoredProcedureName,
            parameters,
            cancellationToken);
    }
}
