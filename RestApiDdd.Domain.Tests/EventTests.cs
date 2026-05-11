using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Events;

namespace RestApiDdd.Domain.Tests;

public sealed class PackageActivationChangedDomainEventTests
{
    [Fact]
    public void Constructor_SetsEventValues_AndBaseOccurredOnUtc()
    {
        var occurredOnUtc = DomainTestData.UtcNow;
        var domainEvent = new PackageActivationChangedDomainEvent(42, true, occurredOnUtc);

        Assert.Equal(42, domainEvent.PackageId);
        Assert.True(domainEvent.IsActive);
        Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc);
        Assert.Equal(occurredOnUtc, ((DomainEvent)domainEvent).OccurredOnUtc);
    }
}
