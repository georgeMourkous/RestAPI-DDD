using RestApiDdd.Domain.Entities;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Dtos;
using RestApiDdd.Service.Exceptions;
using RestApiDdd.Service.Mapping;

namespace RestApiDdd.Service.Packages;

internal sealed class CreatePackageCommandHandler(
    IPackageRepository packageRepository,
    ILookupRepository lookupRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<CreatePackageCommand, PackageDto>
{
    public async Task<PackageDto> HandleAsync(CreatePackageCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureCanUseReferencesAsync(
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
            clock.UtcNow);

        await packageRepository.AddAsync(package, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return package.ToDto(clock.UtcNow);
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
