using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Domain.Tests;

public sealed class PackageCategoryTests
{
    [Fact]
    public void Constructor_SetsTrimmedValues()
    {
        var category = new PackageCategory("  Wellness  ", sortOrder: 2, visible: true);

        Assert.Equal("Wellness", category.Name);
        Assert.Equal(2, category.SortOrder);
        Assert.True(category.Visible);
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var category = new PackageCategory("Wellness", sortOrder: 2, visible: true);

        category.Update("  Dental  ", sortOrder: 0, visible: false);

        Assert.Equal("Dental", category.Name);
        Assert.Equal(0, category.SortOrder);
        Assert.False(category.Visible);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Throws_WhenNameIsMissing(string? name)
    {
        var category = new PackageCategory("Wellness", sortOrder: 2, visible: true);

        Assert.Throws<DomainException>(() => category.Update(name!, sortOrder: 2, visible: true));
    }

    [Fact]
    public void Update_Throws_WhenNameExceedsMaximumLength()
    {
        var category = new PackageCategory("Wellness", sortOrder: 2, visible: true);

        Assert.Throws<DomainException>(() => category.Update(new string('a', 256), sortOrder: 2, visible: true));
    }

    [Fact]
    public void Update_Throws_WhenSortOrderIsNegative()
    {
        var category = new PackageCategory("Wellness", sortOrder: 2, visible: true);

        var exception = Assert.Throws<DomainException>(() => category.Update("Dental", sortOrder: -1, visible: true));

        Assert.Equal("Sort order cannot be negative.", exception.Message);
    }
}
