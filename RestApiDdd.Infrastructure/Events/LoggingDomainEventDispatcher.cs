using Microsoft.Extensions.Logging;
using RestApiDdd.Domain.Common;
using RestApiDdd.Infrastructure.Abstractions;

namespace RestApiDdd.Infrastructure.Events;

internal sealed class LoggingDomainEventDispatcher(
    ILogger<LoggingDomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            logger.LogInformation("Domain event dispatched: {DomainEventType} at {OccurredOnUtc}.",
                domainEvent.GetType().Name,
                domainEvent.OccurredOnUtc);
        }

        return Task.CompletedTask;
    }
}
