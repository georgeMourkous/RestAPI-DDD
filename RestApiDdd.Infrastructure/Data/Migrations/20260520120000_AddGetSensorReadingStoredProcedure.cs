using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestApiDdd.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260520120000_AddGetSensorReadingStoredProcedure")]
    public partial class AddGetSensorReadingStoredProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE [dbo].[GetSensorReading]
                    @StudyId nvarchar(100) = NULL,
                    @SiteId nvarchar(100) = NULL,
                    @SubjectId nvarchar(100) = NULL,
                    @DeviceId nvarchar(100) = NULL,
                    @PageSize int = 10,
                    @PageNumber int = 1
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @EffectivePageSize int = CASE WHEN @PageSize IS NULL OR @PageSize < 1 THEN 10 ELSE @PageSize END;
                    DECLARE @EffectivePageNumber int = CASE WHEN @PageNumber IS NULL OR @PageNumber < 1 THEN 1 ELSE @PageNumber END;

                    SELECT
                        [Id],
                        [MessageId],
                        [StudyId],
                        [SiteId],
                        [SubjectId],
                        [DeviceId],
                        [MeasurementType],
                        [MeasurementTimestampUtc],
                        [ReceivedTimestampUtc],
                        [NumericValue1],
                        [NumericValue2],
                        [Unit],
                        [PayloadJson],
                        [CreatedAtUtc]
                    FROM [dbo].[SensorReading]
                    WHERE (@StudyId IS NULL OR [StudyId] = @StudyId)
                        AND (@SiteId IS NULL OR [SiteId] = @SiteId)
                        AND (@SubjectId IS NULL OR [SubjectId] = @SubjectId)
                        AND (@DeviceId IS NULL OR [DeviceId] = @DeviceId)
                    ORDER BY [Id]
                    OFFSET (@EffectivePageNumber - 1) * @EffectivePageSize ROWS
                    FETCH NEXT @EffectivePageSize ROWS ONLY;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[GetSensorReading];");
        }
    }
}
