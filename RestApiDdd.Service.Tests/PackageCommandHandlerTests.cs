using Moq;
using RestApiDdd.Domain.Entities;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Packages;
using ServiceEntity = RestApiDdd.Domain.Entities.Service;

namespace RestApiDdd.Service.Tests;

public sealed class CreatePackageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesPackageAndReturnsDto_WhenRequestIsValid()
    {
        var context = HandlerTestContext.Create();
        Package? addedPackage = null;
        context.SetupReferences(categoryIds: [1], serviceIds: [101]);
        context.SetupPackageNameExists(exists: false);
        context.SetupAddPackage(package =>
        {
            ServiceTestData.SetEntityId(package, 100);
            addedPackage = package;
        });
        context.SetupSaveChanges();
        var handler = context.CreateHandler();
        var command = new CreatePackageCommand(ServiceTestData.CreatePackageDto(name: "  Premium  "));

        var result = await handler.HandleAsync(command);

        Assert.NotNull(addedPackage);
        Assert.Equal("Premium", addedPackage.Name);
        Assert.Equal(100, result.Id);
        Assert.Equal("Premium", result.Name);
        Assert.True(result.IsActive);
        Assert.Single(result.Frequencies);
        Assert.Single(result.Services);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsConflictException_WhenPackageNameExists()
    {
        var context = HandlerTestContext.Create();
        context.SetupReferences(categoryIds: [1], serviceIds: [101]);
        context.SetupPackageNameExists(exists: true);
        var handler = context.CreateHandler();
        var command = new CreatePackageCommand(ServiceTestData.CreatePackageDto(name: "Starter"));

        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command));

        Assert.Equal("Package 'Starter' already exists.", exception.Message);
        context.PackageRepository.Verify(repository => repository.AddAsync(It.IsAny<Package>(), It.IsAny<CancellationToken>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ThrowsApplicationValidationException_WhenReferencesDoNotExist()
    {
        var context = HandlerTestContext.Create();
        context.SetupReferences(categoryIds: [], serviceIds: []);
        var handler = context.CreateHandler();
        var command = new CreatePackageCommand(ServiceTestData.CreatePackageDto(packageCategoryId: 99));

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(() => handler.HandleAsync(command));

        Assert.Contains("Package category 99 does not exist.", exception.Errors);
        Assert.Contains("Service 101 does not exist.", exception.Errors);
        context.PackageRepository.Verify(repository => repository.AddAsync(It.IsAny<Package>(), It.IsAny<CancellationToken>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

public sealed class UpdatePackageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_UpdatesPackageAndSaves_WhenRequestIsValid()
    {
        var context = HandlerTestContext.Create();
        var package = ServiceTestData.Package(id: 5, name: "Starter");
        context.SetupPackage(5, package);
        context.SetupReferences(categoryIds: [2], serviceIds: [102]);
        context.SetupPackageNameExists("Premium", excludedPackageId: 5, exists: false);
        context.SetupSaveChanges();
        var handler = context.UpdateHandler();
        var command = new UpdatePackageCommand(
            5,
            ServiceTestData.UpdatePackageDto(
                name: "Premium",
                packageCategoryId: 2,
                description: "Updated",
                isQuantityAllowed: false,
                frequencies: [ServiceTestData.FrequencyDto(name: "Weekly", frequency: 7)],
                services: [ServiceTestData.ServiceDto(serviceId: 102, defaultInstances: 2, minimumInstances: 1)]));

        var result = await handler.HandleAsync(command);

        Assert.Equal(default, result);
        Assert.Equal("Premium", package.Name);
        Assert.Equal(2, package.PackageCategoryId);
        Assert.Equal("Updated", package.Description);
        Assert.False(package.IsQuantityAllowed);
        Assert.Contains(package.Frequencies, frequency => frequency.Name == "Weekly" && frequency.Frequency == 7);
        Assert.Contains(package.Services, service => service.ServiceId == 102 && service.DefaultInstances == 2);
        context.PackageRepository.Verify(
            repository => repository.ExistsByNameAsync("Premium", 5, It.IsAny<CancellationToken>()),
            Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotCheckNameConflict_WhenNameOnlyDiffersByCase()
    {
        var context = HandlerTestContext.Create();
        var package = ServiceTestData.Package(id: 5, name: "Starter");
        context.SetupPackage(5, package);
        context.SetupReferences(categoryIds: [1], serviceIds: [101]);
        context.SetupSaveChanges();
        var handler = context.UpdateHandler();
        var command = new UpdatePackageCommand(5, ServiceTestData.UpdatePackageDto(name: "starter"));

        await handler.HandleAsync(command);

        context.PackageRepository.Verify(
            repository => repository.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal("starter", package.Name);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsNotFoundException_WhenPackageDoesNotExist()
    {
        var context = HandlerTestContext.Create();
        context.SetupPackage(404, null);
        var handler = context.UpdateHandler();
        var command = new UpdatePackageCommand(404, ServiceTestData.UpdatePackageDto());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));

        Assert.Equal("Package 404 was not found.", exception.Message);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ThrowsConflictException_WhenUpdatedNameAlreadyExists()
    {
        var context = HandlerTestContext.Create();
        context.SetupPackage(5, ServiceTestData.Package(id: 5, name: "Starter"));
        context.SetupReferences(categoryIds: [1], serviceIds: [101]);
        context.SetupPackageNameExists("Existing", excludedPackageId: 5, exists: true);
        var handler = context.UpdateHandler();
        var command = new UpdatePackageCommand(5, ServiceTestData.UpdatePackageDto(name: "Existing"));

        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command));

        Assert.Equal("Package 'Existing' already exists.", exception.Message);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

public sealed class PatchPackageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_MergesPatchWithExistingPackageAndSaves()
    {
        var context = HandlerTestContext.Create();
        var start = ServiceTestData.UtcNow.AddDays(-3);
        var expire = ServiceTestData.UtcNow.AddDays(3);
        var package = ServiceTestData.Package(
            id: 5,
            name: "Starter",
            description: "Base",
            start: start,
            expire: expire,
            isQuantityAllowed: true);
        SetSingleChildIds(package);
        context.SetupPackage(5, package);
        context.SetupReferences(categoryIds: [2], serviceIds: [101]);
        context.SetupPackageNameExists("Premium", excludedPackageId: 5, exists: false);
        context.SetupSaveChanges();
        var handler = context.PatchHandler();
        var command = new PatchPackageCommand(
            5,
            new RestApiDdd.Service.Dtos.PatchPackageDto
            {
                Name = "Premium",
                PackageCategoryId = 2,
                ClearDescription = true,
                ClearStart = true,
                IsQuantityAllowed = false
            });

        await handler.HandleAsync(command);

        Assert.Equal("Premium", package.Name);
        Assert.Equal(2, package.PackageCategoryId);
        Assert.Null(package.Description);
        Assert.Null(package.Start);
        Assert.Equal(expire, package.Expire);
        Assert.False(package.IsQuantityAllowed);
        Assert.Single(package.Frequencies);
        Assert.Single(package.Services);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ClearsExpireAndUsesProvidedCollections_WhenProvided()
    {
        var context = HandlerTestContext.Create();
        var package = ServiceTestData.Package(id: 5, expire: ServiceTestData.UtcNow.AddDays(3));
        SetSingleChildIds(package);
        context.SetupPackage(5, package);
        context.SetupReferences(categoryIds: [1], serviceIds: [102]);
        context.SetupSaveChanges();
        var handler = context.PatchHandler();
        var command = new PatchPackageCommand(
            5,
            new RestApiDdd.Service.Dtos.PatchPackageDto
            {
                ClearExpire = true,
                Frequencies = [ServiceTestData.FrequencyDto(name: "Weekly", frequency: 7)],
                Services = [ServiceTestData.ServiceDto(serviceId: 102, defaultInstances: 2, minimumInstances: 1)]
            });

        await handler.HandleAsync(command);

        Assert.Null(package.Expire);
        Assert.Contains(package.Frequencies, frequency => frequency.Name == "Weekly");
        Assert.Contains(package.Services, service => service.ServiceId == 102);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsNotFoundException_WhenPackageDoesNotExist()
    {
        var context = HandlerTestContext.Create();
        context.SetupPackage(404, null);
        var handler = context.PatchHandler();
        var command = new PatchPackageCommand(404, new RestApiDdd.Service.Dtos.PatchPackageDto());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));

        Assert.Equal("Package 404 was not found.", exception.Message);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ThrowsConflictException_WhenPatchedNameAlreadyExists()
    {
        var context = HandlerTestContext.Create();
        context.SetupPackage(5, ServiceTestData.Package(id: 5, name: "Starter"));
        context.SetupReferences(categoryIds: [1], serviceIds: [101]);
        context.SetupPackageNameExists("Existing", excludedPackageId: 5, exists: true);
        var handler = context.PatchHandler();
        var command = new PatchPackageCommand(
            5,
            new RestApiDdd.Service.Dtos.PatchPackageDto
            {
                Name = "Existing"
            });

        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command));

        Assert.Equal("Package 'Existing' already exists.", exception.Message);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SetSingleChildIds(Package package)
    {
        ServiceTestData.SetEntityId(package.Frequencies.Single(), 10);
        ServiceTestData.SetEntityId(package.Services.Single(), 20);
    }
}

public sealed class DeletePackageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_RemovesPackageAndSaves_WhenPackageExists()
    {
        var context = HandlerTestContext.Create();
        var package = ServiceTestData.Package(id: 5);
        context.SetupPackage(5, package);
        context.PackageRepository
            .Setup(repository => repository.Remove(package));
        context.SetupSaveChanges();
        var handler = context.DeleteHandler();

        var result = await handler.HandleAsync(new DeletePackageCommand(5));

        Assert.Equal(default, result);
        context.PackageRepository.Verify(repository => repository.Remove(package), Times.Once);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsNotFoundException_WhenPackageDoesNotExist()
    {
        var context = HandlerTestContext.Create();
        context.SetupPackage(404, null);
        var handler = context.DeleteHandler();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new DeletePackageCommand(404)));

        Assert.Equal("Package 404 was not found.", exception.Message);
        context.PackageRepository.Verify(repository => repository.Remove(It.IsAny<Package>()), Times.Never);
        context.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

internal sealed class HandlerTestContext
{
    private HandlerTestContext()
    {
        Clock.SetupGet(clock => clock.UtcNow)
            .Returns(ServiceTestData.UtcNow);
    }

    public Mock<IPackageRepository> PackageRepository { get; } = new(MockBehavior.Strict);

    public Mock<IServiceRepository> ServiceRepository { get; } = new(MockBehavior.Strict);

    public Mock<IUnitOfWork> UnitOfWork { get; } = new(MockBehavior.Strict);

    public Mock<IClock> Clock { get; } = new(MockBehavior.Strict);

    public static HandlerTestContext Create()
    {
        return new HandlerTestContext();
    }

    public void SetupPackage(int id, Package? package)
    {
        PackageRepository
            .Setup(repository => repository.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
    }

    public void SetupReferences(int[] categoryIds, int[] serviceIds)
    {
        PackageRepository
            .Setup(repository => repository.CategoryExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((id, _) => Task.FromResult(categoryIds.Contains(id)));

        IReadOnlyList<ServiceEntity> services = serviceIds
            .Select(id => ServiceTestData.Service(id))
            .ToArray();

        ServiceRepository
            .Setup(repository => repository.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(services);
    }

    public void SetupPackageNameExists(bool exists)
    {
        PackageRepository
            .Setup(repository => repository.ExistsByNameAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
    }

    public void SetupPackageNameExists(string name, int? excludedPackageId, bool exists)
    {
        PackageRepository
            .Setup(repository => repository.ExistsByNameAsync(name, excludedPackageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
    }

    public void SetupAddPackage(Action<Package> onAdd)
    {
        PackageRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Package>(), It.IsAny<CancellationToken>()))
            .Callback<Package, CancellationToken>((package, _) => onAdd(package))
            .Returns(Task.CompletedTask);
    }

    public void SetupSaveChanges()
    {
        UnitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    public CreatePackageCommandHandler CreateHandler()
    {
        return new CreatePackageCommandHandler(PackageRepository.Object, UnitOfWork.Object, Clock.Object, ReferenceValidator());
    }

    public UpdatePackageCommandHandler UpdateHandler()
    {
        return new UpdatePackageCommandHandler(PackageRepository.Object, UnitOfWork.Object, Clock.Object, ReferenceValidator());
    }

    public PatchPackageCommandHandler PatchHandler()
    {
        return new PatchPackageCommandHandler(PackageRepository.Object, UnitOfWork.Object, Clock.Object, ReferenceValidator());
    }

    public DeletePackageCommandHandler DeleteHandler()
    {
        return new DeletePackageCommandHandler(PackageRepository.Object, UnitOfWork.Object);
    }

    private PackageReferenceValidator ReferenceValidator()
    {
        return new PackageReferenceValidator(PackageRepository.Object, ServiceRepository.Object);
    }
}
