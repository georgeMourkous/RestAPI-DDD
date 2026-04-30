using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Mapping;

namespace RestApiDdd.Service.Packages;

internal sealed class PatchPackageCommandHandler(
    IPackageRepository packageRepository,
    ILookupRepository lookupRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<PatchPackageCommand, Unit>
{
    public async Task<Unit> HandleAsync(PatchPackageCommand command, CancellationToken cancellationToken = default)
    {
        var package = await packageRepository.GetByIdWithDetailsAsync(command.Id, cancellationToken);
        if (package is null)
        {
            throw new NotFoundException($"Package {command.Id} was not found.");
        }

        var categoryId = command.Package.PackageCategoryId ?? package.PackageCategoryId;
        var serviceDetails = command.Package.Services ?? package.Services
            .Select(service => new Dtos.PackageServiceDto
            {
                Id = service.Id,
                ServiceId = service.ServiceId,
                DefaultInstances = service.DefaultInstances,
                MinimumInstances = service.MinimumInstances,
                MaximumInstances = service.MaximumInstances
            })
            .ToList();

        await EnsureCanUseReferencesAsync(categoryId, serviceDetails.Select(service => service.ServiceId), cancellationToken);

        var name = command.Package.Name ?? package.Name;
        if (!string.Equals(name, package.Name, StringComparison.OrdinalIgnoreCase)
            && await packageRepository.ExistsByNameAsync(name, package.Id, cancellationToken))
        {
            throw new ConflictException($"Package '{name}' already exists.");
        }

        var frequencyDetails = command.Package.Frequencies ?? package.Frequencies
            .Select(frequency => new Dtos.PackageFrequencyDto
            {
                Id = frequency.Id,
                Name = frequency.Name,
                Frequency = frequency.Frequency,
                IsActive = frequency.IsActive,
                Created = frequency.Created
            })
            .ToList();

        package.Update(
            name,
            categoryId,
            command.Package.ClearDescription ? null : command.Package.Description ?? package.Description,
            command.Package.ClearStart ? null : command.Package.Start ?? package.Start,
            command.Package.ClearExpire ? null : command.Package.Expire ?? package.Expire,
            command.Package.IsQuantityAllowed ?? package.IsQuantityAllowed,
            frequencyDetails.ToFrequencyDefinitions(),
            serviceDetails.ToServiceDefinitions(),
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
