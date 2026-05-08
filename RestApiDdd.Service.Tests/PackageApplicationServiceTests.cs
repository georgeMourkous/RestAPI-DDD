using Moq;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Packages;

namespace RestApiDdd.Service.Tests;

public sealed class PackageApplicationServiceTests
{
    [Fact]
    public async Task GetPackagesAsync_DelegatesToGetPackagesQueryHandler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        IReadOnlyList<PackageDto> expected = [new PackageDto { Id = 1, Name = "Starter" }];
        var harness = new PackageApplicationServiceHarness();
        harness.GetPackagesHandler
            .Setup(handler => handler.HandleAsync(It.IsAny<GetPackagesQuery>(), cancellationTokenSource.Token))
            .ReturnsAsync(expected);

        var result = await harness.Service.GetPackagesAsync(cancellationTokenSource.Token);

        Assert.Same(expected, result);
        harness.GetPackagesHandler.Verify(
            handler => handler.HandleAsync(It.IsAny<GetPackagesQuery>(), cancellationTokenSource.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetPackageAsync_DelegatesToGetPackageByIdQueryHandler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var expected = new PackageDto { Id = 5, Name = "Starter" };
        var harness = new PackageApplicationServiceHarness();
        harness.GetPackageByIdHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<GetPackageByIdQuery>(query => query.Id == 5),
                cancellationTokenSource.Token))
            .ReturnsAsync(expected);

        var result = await harness.Service.GetPackageAsync(5, cancellationTokenSource.Token);

        Assert.Same(expected, result);
        harness.GetPackageByIdHandler.Verify(
            handler => handler.HandleAsync(
                It.Is<GetPackageByIdQuery>(query => query.Id == 5),
                cancellationTokenSource.Token),
            Times.Once);
    }

    [Fact]
    public async Task CreatePackageAsync_DelegatesToCreatePackageCommandHandler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var request = ServiceTestData.CreatePackageDto(name: "Starter");
        var expected = new PackageDto { Id = 5, Name = "Starter" };
        var harness = new PackageApplicationServiceHarness();
        harness.CreateHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<CreatePackageCommand>(command => ReferenceEquals(command.Package, request)),
                cancellationTokenSource.Token))
            .ReturnsAsync(expected);

        var result = await harness.Service.CreatePackageAsync(request, cancellationTokenSource.Token);

        Assert.Same(expected, result);
        harness.CreateHandler.Verify(
            handler => handler.HandleAsync(
                It.Is<CreatePackageCommand>(command => ReferenceEquals(command.Package, request)),
                cancellationTokenSource.Token),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePackageAsync_DelegatesToUpdatePackageCommandHandler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var request = ServiceTestData.UpdatePackageDto(name: "Starter");
        var harness = new PackageApplicationServiceHarness();
        harness.UpdateHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<UpdatePackageCommand>(command => command.Id == 5 && ReferenceEquals(command.Package, request)),
                cancellationTokenSource.Token))
            .ReturnsAsync(Unit.Value);

        await harness.Service.UpdatePackageAsync(5, request, cancellationTokenSource.Token);

        harness.UpdateHandler.Verify(
            handler => handler.HandleAsync(
                It.Is<UpdatePackageCommand>(command => command.Id == 5 && ReferenceEquals(command.Package, request)),
                cancellationTokenSource.Token),
            Times.Once);
    }

    [Fact]
    public async Task PatchPackageAsync_DelegatesToPatchPackageCommandHandler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var request = new PatchPackageDto { Name = "Starter" };
        var harness = new PackageApplicationServiceHarness();
        harness.PatchHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<PatchPackageCommand>(command => command.Id == 5 && ReferenceEquals(command.Package, request)),
                cancellationTokenSource.Token))
            .ReturnsAsync(Unit.Value);

        await harness.Service.PatchPackageAsync(5, request, cancellationTokenSource.Token);

        harness.PatchHandler.Verify(
            handler => handler.HandleAsync(
                It.Is<PatchPackageCommand>(command => command.Id == 5 && ReferenceEquals(command.Package, request)),
                cancellationTokenSource.Token),
            Times.Once);
    }

    [Fact]
    public async Task DeletePackageAsync_DelegatesToDeletePackageCommandHandler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var harness = new PackageApplicationServiceHarness();
        harness.DeleteHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<DeletePackageCommand>(command => command.Id == 5),
                cancellationTokenSource.Token))
            .ReturnsAsync(Unit.Value);

        await harness.Service.DeletePackageAsync(5, cancellationTokenSource.Token);

        harness.DeleteHandler.Verify(
            handler => handler.HandleAsync(
                It.Is<DeletePackageCommand>(command => command.Id == 5),
                cancellationTokenSource.Token),
            Times.Once);
    }

    private sealed class PackageApplicationServiceHarness
    {
        public PackageApplicationServiceHarness()
        {
            Service = new PackageApplicationService(
                GetPackagesHandler.Object,
                GetPackageByIdHandler.Object,
                CreateHandler.Object,
                UpdateHandler.Object,
                PatchHandler.Object,
                DeleteHandler.Object);
        }

        public PackageApplicationService Service { get; }

        public Mock<IQueryHandler<GetPackagesQuery, IReadOnlyList<PackageDto>>> GetPackagesHandler { get; } = new(MockBehavior.Strict);

        public Mock<IQueryHandler<GetPackageByIdQuery, PackageDto>> GetPackageByIdHandler { get; } = new(MockBehavior.Strict);

        public Mock<ICommandHandler<CreatePackageCommand, PackageDto>> CreateHandler { get; } = new(MockBehavior.Strict);

        public Mock<ICommandHandler<UpdatePackageCommand, Unit>> UpdateHandler { get; } = new(MockBehavior.Strict);

        public Mock<ICommandHandler<PatchPackageCommand, Unit>> PatchHandler { get; } = new(MockBehavior.Strict);

        public Mock<ICommandHandler<DeletePackageCommand, Unit>> DeleteHandler { get; } = new(MockBehavior.Strict);
    }
}
