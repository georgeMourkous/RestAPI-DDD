using RestApiDdd.Domain.Common;

namespace RestApiDdd.Service.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
