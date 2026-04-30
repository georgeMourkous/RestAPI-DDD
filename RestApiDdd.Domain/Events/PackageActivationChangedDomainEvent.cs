using RestApiDdd.Domain.Common;

namespace RestApiDdd.Domain.Events;

public sealed record PackageActivationChangedDomainEvent(
    int PackageId,
    bool IsActive,
    DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
