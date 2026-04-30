using RestApiDdd.Service.Abstractions;
using RestApiDdd.Service.Cqrs;
using RestApiDdd.Service.Exceptions;

namespace RestApiDdd.Service.Packages;

internal sealed class DeletePackageCommandHandler(
    IPackageRepository packageRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeletePackageCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeletePackageCommand command, CancellationToken cancellationToken = default)
    {
        var package = await packageRepository.GetByIdWithDetailsAsync(command.Id, cancellationToken);
        if (package is null)
        {
            throw new NotFoundException($"Package {command.Id} was not found.");
        }

        packageRepository.Remove(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
