using Moq;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Packages;

namespace RestApiDdd.Service.Tests;

public sealed class GetPackageByIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsPackageDto_WhenPackageExists()
    {
        var repository = new Mock<IPackageRepository>(MockBehavior.Strict);
        var clock = new Mock<IClock>(MockBehavior.Strict);
        var package = ServiceTestData.Package(id: 5, name: "Starter", expire: ServiceTestData.UtcNow);
        repository
            .Setup(packageRepository => packageRepository.GetByIdWithDetailsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        clock.SetupGet(systemClock => systemClock.UtcNow)
            .Returns(ServiceTestData.UtcNow);
        var handler = new GetPackageByIdQueryHandler(repository.Object, clock.Object);

        var result = await handler.HandleAsync(new GetPackageByIdQuery(5));

        Assert.Equal(5, result.Id);
        Assert.Equal("Starter", result.Name);
        Assert.True(result.IsActive);
        Assert.Single(result.Frequencies);
        Assert.Single(result.Services);
        repository.Verify(
            packageRepository => packageRepository.GetByIdWithDetailsAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsNotFoundException_WhenPackageDoesNotExist()
    {
        var repository = new Mock<IPackageRepository>(MockBehavior.Strict);
        repository
            .Setup(packageRepository => packageRepository.GetByIdWithDetailsAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RestApiDdd.Domain.Entities.Package?)null);
        var handler = new GetPackageByIdQueryHandler(repository.Object, Mock.Of<IClock>());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new GetPackageByIdQuery(404)));

        Assert.Equal("Package 404 was not found.", exception.Message);
    }
}

public sealed class GetPackagesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedPackages()
    {
        var repository = new Mock<IPackageRepository>(MockBehavior.Strict);
        var clock = new Mock<IClock>(MockBehavior.Strict);
        IReadOnlyList<RestApiDdd.Domain.Entities.Package> packages =
        [
            ServiceTestData.Package(id: 1, name: "Active", start: ServiceTestData.UtcNow.AddDays(-1)),
            ServiceTestData.Package(id: 2, name: "Future", start: ServiceTestData.UtcNow.AddDays(1))
        ];
        repository
            .Setup(packageRepository => packageRepository.ListWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(packages);
        clock.SetupGet(systemClock => systemClock.UtcNow)
            .Returns(ServiceTestData.UtcNow);
        var handler = new GetPackagesQueryHandler(repository.Object, clock.Object);

        var result = await handler.HandleAsync(new GetPackagesQuery());

        Assert.Collection(
            result,
            package =>
            {
                Assert.Equal(1, package.Id);
                Assert.Equal("Active", package.Name);
                Assert.True(package.IsActive);
            },
            package =>
            {
                Assert.Equal(2, package.Id);
                Assert.Equal("Future", package.Name);
                Assert.False(package.IsActive);
            });
        repository.Verify(
            packageRepository => packageRepository.ListWithDetailsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
