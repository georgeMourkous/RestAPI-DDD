using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Domain.Tests;

public sealed class PackageServiceTests
{
    [Fact]
    public void Create_SetsValues()
    {
        var service = PackageService.Create(serviceId: 101, defaultInstances: 2, minimumInstances: 1, maximumInstances: 4);

        Assert.Equal(101, service.ServiceId);
        Assert.Equal(2, service.DefaultInstances);
        Assert.Equal(1, service.MinimumInstances);
        Assert.Equal(4, service.MaximumInstances);
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var service = PackageService.Create(serviceId: 101, defaultInstances: 2, minimumInstances: 1, maximumInstances: 4);

        service.Update(serviceId: 102, defaultInstances: 3, minimumInstances: 2, maximumInstances: null);

        Assert.Equal(102, service.ServiceId);
        Assert.Equal(3, service.DefaultInstances);
        Assert.Equal(2, service.MinimumInstances);
        Assert.Null(service.MaximumInstances);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Throws_WhenServiceIdIsNotGreaterThanZero(int serviceId)
    {
        var exception = Assert.Throws<DomainException>(() =>
            PackageService.Create(serviceId, defaultInstances: 1, minimumInstances: 0, maximumInstances: 2));

        Assert.Equal("ServiceId must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenMinimumInstancesIsNegative()
    {
        var exception = Assert.Throws<DomainException>(() =>
            PackageService.Create(serviceId: 101, defaultInstances: 1, minimumInstances: -1, maximumInstances: 2));

        Assert.Equal("Minimum instances cannot be negative.", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenDefaultInstancesIsLessThanMinimumInstances()
    {
        var exception = Assert.Throws<DomainException>(() =>
            PackageService.Create(serviceId: 101, defaultInstances: 1, minimumInstances: 2, maximumInstances: 3));

        Assert.Equal("Default instances cannot be less than minimum instances.", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenMaximumInstancesIsLessThanDefaultInstances()
    {
        var exception = Assert.Throws<DomainException>(() =>
            PackageService.Create(serviceId: 101, defaultInstances: 2, minimumInstances: 1, maximumInstances: 1));

        Assert.Equal("Maximum instances cannot be less than default instances.", exception.Message);
    }
}
