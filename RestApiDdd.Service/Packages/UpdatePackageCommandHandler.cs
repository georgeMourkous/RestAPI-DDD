using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Mapping;

namespace RestApiDdd.Service.Packages;

internal sealed class UpdatePackageCommandHandler(
    IPackageRepository packageRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    PackageReferenceValidator referenceValidator) : ICommandHandler<UpdatePackageCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdatePackageCommand command, CancellationToken cancellationToken = default)
    {
        var package = await packageRepository.GetByIdWithDetailsAsync(command.Id, cancellationToken);
        if (package is null)
        {
            throw new NotFoundException($"Package {command.Id} was not found.");
        }

        await referenceValidator.EnsureCanUseReferencesAsync(
            command.Package.PackageCategoryId,
            command.Package.Services.Select(service => service.ServiceId),
            cancellationToken);

        if (!string.Equals(command.Package.Name, package.Name, StringComparison.OrdinalIgnoreCase)
            && await packageRepository.ExistsByNameAsync(command.Package.Name, package.Id, cancellationToken))
        {
            throw new ConflictException($"Package '{command.Package.Name}' already exists.");
        }

        package.Update(
            command.Package.Name,
            command.Package.PackageCategoryId,
            command.Package.Description,
            command.Package.Start,
            command.Package.Expire,
            command.Package.IsQuantityAllowed,
            command.Package.Frequencies.ToFrequencyDefinitions(),
            command.Package.Services.ToServiceDefinitions(),
            clock.UtcNow,
            command.Package.FullPeriod ?? package.FullPeriod,
            command.Package.PostPaid ?? package.PostPaid);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
