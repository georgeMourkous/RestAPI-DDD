using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Domain.Tests;

public sealed class ServiceStatusTypeTests
{
    [Fact]
    public void Constructor_SetsTrimmedValues()
    {
        var statusType = new ServiceStatusType("  Recurring Charge  ", sortOrder: 1, "  mrc  ");

        Assert.Equal("Recurring Charge", statusType.Name);
        Assert.Equal(1, statusType.SortOrder);
        Assert.Equal("mrc", statusType.TokenName);
    }

    [Fact]
    public void Constructor_Throws_WhenSortOrderIsNegative()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new ServiceStatusType("Recurring Charge", sortOrder: -1, "mrc"));

        Assert.Equal("Sort order cannot be negative.", exception.Message);
    }

    [Fact]
    public void Constructor_Throws_WhenTokenNameExceedsMaximumLength()
    {
        Assert.Throws<DomainException>(() =>
            new ServiceStatusType("Recurring Charge", sortOrder: 1, new string('a', 51)));
    }
}
