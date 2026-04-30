using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Mapping;

namespace RestApiDdd.Service.Packages;

internal sealed class UpdatePackageCommandHandler(
    IPackageRepository packageRepository,
    ILookupRepository lookupRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<UpdatePackageCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdatePackageCommand command, CancellationToken cancellationToken = default)
    {
        var package = await packageRepository.GetByIdWithDetailsAsync(command.Id, cancellationToken);
        if (package is null)
        {
            throw new NotFoundException($"Package {command.Id} was not found.");
        }

        await EnsureCanUseReferencesAsync(
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
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureCanUseReferencesAsync(
        int packageCategoryId,
        IEnumerable<int> serviceIds,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (!await lookupRepository.PackageCategoryExistsAsync(packageCategoryId, cancellationToken))
        {
            errors.Add($"Package category {packageCategoryId} does not exist.");
        }

        var requestedServiceIds = serviceIds.Distinct().ToArray();
        var existingServiceIds = await lookupRepository.GetExistingServiceIdsAsync(requestedServiceIds, cancellationToken);
        errors.AddRange(requestedServiceIds
            .Where(serviceId => !existingServiceIds.Contains(serviceId))
            .Select(serviceId => $"Service {serviceId} does not exist."));

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }
    }
}
