using Moq;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.SensorReadings;

namespace RestApiDdd.Service.Tests;

public sealed class SensorReadingQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_NormalizesSearchRequestAndReturnsRepositoryRows()
    {
        var repository = new Mock<ISensorReadingRepository>(MockBehavior.Strict);
        SearchRequest? capturedRequest = null;
        IReadOnlyList<SensorReadingDto> rows =
        [
            new SensorReadingDto
            {
                Id = 1,
                MessageId = Guid.NewGuid(),
                StudyId = "ST-001",
                SubjectId = "SUB-100",
                DeviceId = "DEV-100",
                MeasurementType = "Temperature",
                MeasurementTimestampUtc = ServiceTestData.UtcNow,
                ReceivedTimestampUtc = ServiceTestData.UtcNow,
                CreatedAtUtc = ServiceTestData.UtcNow
            }
        ];
        repository
            .Setup(sensorReadingRepository => sensorReadingRepository.SearchAsync(
                It.IsAny<SearchRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<SearchRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(rows);
        var handler = new GetSensorReadingsQueryHandler(repository.Object);

        var result = await handler.HandleAsync(new GetSensorReadingsQuery(new SearchRequest
        {
            StudyId = " ST-001 ",
            SiteId = " ",
            DeviceId = "DEV-100",
            PageSize = null,
            PageNumber = null
        }));

        Assert.Same(rows, result);
        Assert.NotNull(capturedRequest);
        Assert.Equal("ST-001", capturedRequest.StudyId);
        Assert.Null(capturedRequest.SiteId);
        Assert.Null(capturedRequest.SubjectId);
        Assert.Equal("DEV-100", capturedRequest.DeviceId);
        Assert.Equal(10, capturedRequest.PageSize);
        Assert.Equal(1, capturedRequest.PageNumber);
    }

    [Theory]
    [InlineData(0, 1, "PageSize must be greater than zero.")]
    [InlineData(10, 0, "PageNumber must be greater than zero.")]
    public async Task HandleAsync_ThrowsValidationException_WhenPaginationIsInvalid(
        int pageSize,
        int pageNumber,
        string expectedError)
    {
        var handler = new GetSensorReadingsQueryHandler(Mock.Of<ISensorReadingRepository>());

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            handler.HandleAsync(new GetSensorReadingsQuery(new SearchRequest
            {
                PageSize = pageSize,
                PageNumber = pageNumber
            })));

        Assert.Contains(expectedError, exception.Errors);
    }
}
