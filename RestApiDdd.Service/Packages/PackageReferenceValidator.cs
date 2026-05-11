using RestApiDdd.Domain.Entities;
using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Exceptions;

namespace RestApiDdd.Service.Packages;

internal sealed class PackageReferenceValidator(
    IServiceRepository serviceRepository)
{
    public async Task EnsureCanUseReferencesAsync(
        int packageCategoryId,
        IEnumerable<int> serviceIds,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined((PackageCategoryType)packageCategoryId))
        {
            errors.Add($"Package category {packageCategoryId} does not exist.");
        }

        var requestedServiceIds = serviceIds.Distinct().ToArray();
        if (requestedServiceIds.Length > 0)
        {
            var existingServiceIds = (await serviceRepository.ListAsync(cancellationToken))
                .Select(service => service.Id)
                .ToHashSet();

            errors.AddRange(requestedServiceIds
                .Where(serviceId => !existingServiceIds.Contains(serviceId))
                .Select(serviceId => $"Service {serviceId} does not exist."));
        }

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }
    }
}
