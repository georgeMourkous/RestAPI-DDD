using RestApiDdd.Domain.Common;

namespace RestApiDdd.Infrastructure.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
