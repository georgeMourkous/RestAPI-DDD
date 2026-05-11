using RestApiDdd.Domain.Common;

namespace RestApiDdd.Domain.Tests;

public sealed class GuardTests
{
    [Fact]
    public void RequiredMaxLength_ReturnsTrimmedValue_WhenValueIsPresent()
    {
        var result = Guard.RequiredMaxLength("  Starter  ", "Name", 20);

        Assert.Equal("Starter", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequiredMaxLength_Throws_WhenValueIsMissing(string? value)
    {
        var exception = Assert.Throws<DomainException>(() => Guard.RequiredMaxLength(value, "Name", 20));

        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public void RequiredMaxLength_Throws_WhenTrimmedValueExceedsMaximumLength()
    {
        var exception = Assert.Throws<DomainException>(() => Guard.RequiredMaxLength("abcdef", "Name", 5));

        Assert.Equal("Name cannot exceed 5 characters.", exception.Message);
    }

    [Fact]
    public void OptionalMaxLength_ReturnsNull_WhenValueIsNull()
    {
        var result = Guard.OptionalMaxLength(null, "Description", 20);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void OptionalMaxLength_ReturnsNull_WhenTrimmedValueIsEmpty(string value)
    {
        var result = Guard.OptionalMaxLength(value, "Description", 20);

        Assert.Null(result);
    }

    [Fact]
    public void OptionalMaxLength_ReturnsTrimmedValue_WhenValueIsPresent()
    {
        var result = Guard.OptionalMaxLength("  useful text  ", "Description", 20);

        Assert.Equal("useful text", result);
    }

    [Fact]
    public void OptionalMaxLength_Throws_WhenTrimmedValueExceedsMaximumLength()
    {
        var exception = Assert.Throws<DomainException>(() => Guard.OptionalMaxLength("abcdef", "Description", 5));

        Assert.Equal("Description cannot exceed 5 characters.", exception.Message);
    }

    [Fact]
    public void PositiveId_ReturnsValue_WhenValueIsGreaterThanZero()
    {
        var result = Guard.PositiveId(7, "PackageCategoryId");

        Assert.Equal(7, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PositiveId_Throws_WhenValueIsNotGreaterThanZero(int value)
    {
        var exception = Assert.Throws<DomainException>(() => Guard.PositiveId(value, "PackageCategoryId"));

        Assert.Equal("PackageCategoryId must be greater than zero.", exception.Message);
    }
}

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_SetsExceptionMessage()
    {
        var exception = new DomainException("Domain rule failed.");

        Assert.Equal("Domain rule failed.", exception.Message);
    }
}

public sealed class EntityTests
{
    [Fact]
    public void Id_ReturnsValueAssignedByDerivedEntity()
    {
        var entity = new TestEntity(42);

        Assert.Equal(42, entity.Id);
    }

    private sealed class TestEntity : Entity
    {
        public TestEntity(int id)
        {
            Id = id;
        }
    }
}

public sealed class AggregateRootTests
{
    [Fact]
    public void DomainEvents_ReturnsRaisedEvents_AndClearDomainEventsRemovesThem()
    {
        var aggregate = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent(DomainTestData.UtcNow);

        aggregate.Publish(domainEvent);

        var raisedEvent = Assert.Single(aggregate.DomainEvents);
        Assert.Same(domainEvent, raisedEvent);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    private sealed class TestAggregateRoot : AggregateRoot
    {
        public void Publish(DomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }

    private sealed record TestDomainEvent(DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
}
