using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Domain.Tests;

public sealed class PackageFrequencyTests
{
    [Fact]
    public void Create_SetsTrimmedValuesAndCreatedDate()
    {
        var created = DomainTestData.UtcNow;

        var frequency = PackageFrequency.Create("  Monthly  ", 30, isActive: true, created);

        Assert.Equal("Monthly", frequency.Name);
        Assert.Equal(30, frequency.Frequency);
        Assert.True(frequency.IsActive);
        Assert.Equal(created, frequency.Created);
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var frequency = PackageFrequency.Create("Monthly", 30, isActive: true, DomainTestData.UtcNow);

        frequency.Update("  Yearly  ", 365, isActive: false);

        Assert.Equal("Yearly", frequency.Name);
        Assert.Equal(365, frequency.Frequency);
        Assert.False(frequency.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsMissing(string? name)
    {
        Assert.Throws<DomainException>(() => PackageFrequency.Create(name!, 30, isActive: true, DomainTestData.UtcNow));
    }

    [Fact]
    public void Update_Throws_WhenNameExceedsMaximumLength()
    {
        var frequency = PackageFrequency.Create("Monthly", 30, isActive: true, DomainTestData.UtcNow);

        Assert.Throws<DomainException>(() => frequency.Update(new string('a', 256), 30, isActive: true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Throws_WhenFrequencyIsNotGreaterThanZero(int value)
    {
        var exception = Assert.Throws<DomainException>(() =>
            PackageFrequency.Create("Monthly", value, isActive: true, DomainTestData.UtcNow));

        Assert.Equal("Frequency must be greater than zero.", exception.Message);
    }
}
