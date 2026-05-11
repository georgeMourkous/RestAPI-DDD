using RestApiDdd.Domain.Entities;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Mapping;

namespace RestApiDdd.Service.Packages;

internal sealed class CreatePackageCommandHandler(
    IPackageRepository packageRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    PackageReferenceValidator referenceValidator) : ICommandHandler<CreatePackageCommand, PackageDto>
{
    public async Task<PackageDto> HandleAsync(CreatePackageCommand command, CancellationToken cancellationToken = default)
    {
        await referenceValidator.EnsureCanUseReferencesAsync(
            command.Package.PackageCategoryId,
            command.Package.Services.Select(service => service.ServiceId),
            cancellationToken);

        if (await packageRepository.ExistsByNameAsync(command.Package.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Package '{command.Package.Name}' already exists.");
        }

        var package = Package.Create(
            command.Package.Name,
            command.Package.PackageCategoryId,
            command.Package.Description,
            command.Package.Start,
            command.Package.Expire,
            command.Package.IsQuantityAllowed,
            command.Package.Frequencies.ToFrequencyDefinitions(),
            command.Package.Services.ToServiceDefinitions(),
            clock.UtcNow,
            command.Package.FullPeriod ?? false,
            command.Package.PostPaid ?? false);

        await packageRepository.AddAsync(package, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return package.ToDto(clock.UtcNow);
    }
}
