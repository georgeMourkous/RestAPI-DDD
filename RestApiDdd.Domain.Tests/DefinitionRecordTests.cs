using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Domain.Tests;

public sealed class DefinitionRecordTests
{
    [Fact]
    public void PackageFrequencyDefinition_ExposesValues_AndUsesValueEquality()
    {
        var definition = new PackageFrequencyDefinition(5, "Monthly", 30, true);

        Assert.Equal(5, definition.Id);
        Assert.Equal("Monthly", definition.Name);
        Assert.Equal(30, definition.Frequency);
        Assert.True(definition.IsActive);
        Assert.Equal(definition, definition with { });
        Assert.NotEqual(definition, definition with { Frequency = 31 });
    }

    [Fact]
    public void PackageServiceDefinition_ExposesValues_AndUsesValueEquality()
    {
        var definition = new PackageServiceDefinition(7, 101, 2, 1, 4);

        Assert.Equal(7, definition.Id);
        Assert.Equal(101, definition.ServiceId);
        Assert.Equal(2, definition.DefaultInstances);
        Assert.Equal(1, definition.MinimumInstances);
        Assert.Equal(4, definition.MaximumInstances);
        Assert.Equal(definition, definition with { });
        Assert.NotEqual(definition, definition with { MaximumInstances = 5 });
    }
}
