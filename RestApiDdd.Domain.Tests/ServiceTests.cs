using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Domain.Tests;

public sealed class ServiceTests
{
    [Fact]
    public void Constructor_SetsTrimmedValues()
    {
        var service = new Service("  Consultation  ", "  Initial visit  ");

        Assert.Equal("Consultation", service.Name);
        Assert.Equal("Initial visit", service.Description);
        Assert.Empty(service.DomainEvents);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConvertsMissingDescriptionToNull(string? description)
    {
        var service = new Service("Consultation", description);

        Assert.Null(service.Description);
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var service = new Service("Consultation", "Initial visit");

        service.Update("  Follow up  ", "  Second visit  ");

        Assert.Equal("Follow up", service.Name);
        Assert.Equal("Second visit", service.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Throws_WhenNameIsMissing(string? name)
    {
        var service = new Service("Consultation", "Initial visit");

        Assert.Throws<DomainException>(() => service.Update(name!, "Initial visit"));
    }

    [Fact]
    public void Update_Throws_WhenNameExceedsMaximumLength()
    {
        var service = new Service("Consultation", "Initial visit");

        Assert.Throws<DomainException>(() => service.Update(new string('a', 256), "Initial visit"));
    }

    [Fact]
    public void Update_Throws_WhenDescriptionExceedsMaximumLength()
    {
        var service = new Service("Consultation", "Initial visit");

        Assert.Throws<DomainException>(() => service.Update("Consultation", new string('a', 2001)));
    }
}
