using RestApiDdd.Domain.Common;
using RestApiDdd.Domain.Entities;
using RestApiDdd.Domain.Events;

namespace RestApiDdd.Domain.Tests;

public sealed class PackageTests
{
    [Fact]
    public void Create_SetsPackageValuesAndChildCollections()
    {
        var now = DomainTestData.UtcNow;
        var start = now.AddDays(-1);
        var expire = now.AddDays(1);

        var package = Package.Create(
            "  Premium  ",
            packageCategoryId: (int)PackageCategoryType.GlobalAddOn,
            description: "  Includes extras  ",
            start,
            expire,
            isQuantityAllowed: true,
            [
                DomainTestData.FrequencyDefinition(name: "  Monthly  ", frequency: 30),
                DomainTestData.FrequencyDefinition(name: "Yearly", frequency: 365, isActive: false)
            ],
            [
                DomainTestData.ServiceDefinition(serviceId: 101, defaultInstances: 2, minimumInstances: 1, maximumInstances: 5),
                DomainTestData.ServiceDefinition(serviceId: 102, defaultInstances: 1, minimumInstances: 0, maximumInstances: null)
            ],
            now,
            fullPeriod: true,
            postPaid: true);

        Assert.Equal("Premium", package.Name);
        Assert.Equal(6, package.PackageCategoryId);
        Assert.Equal(PackageCategoryType.GlobalAddOn, package.PackageCategoryType);
        Assert.Equal("Includes extras", package.Description);
        Assert.Equal(now, package.Created);
        Assert.Equal(start, package.Start);
        Assert.Equal(expire, package.Expire);
        Assert.True(package.IsQuantityAllowed);
        Assert.True(package.FullPeriod);
        Assert.True(package.PostPaid);
        Assert.Empty(package.DomainEvents);

        Assert.Collection(
            package.Frequencies,
            frequency =>
            {
                Assert.Equal("Monthly", frequency.Name);
                Assert.Equal(30, frequency.Frequency);
                Assert.True(frequency.IsActive);
                Assert.Equal(now, frequency.Created);
            },
            frequency =>
            {
                Assert.Equal("Yearly", frequency.Name);
                Assert.Equal(365, frequency.Frequency);
                Assert.False(frequency.IsActive);
            });

        Assert.Collection(
            package.Services,
            service =>
            {
                Assert.Equal(101, service.ServiceId);
                Assert.Equal(2, service.DefaultInstances);
                Assert.Equal(1, service.MinimumInstances);
                Assert.Equal(5, service.MaximumInstances);
            },
            service =>
            {
                Assert.Equal(102, service.ServiceId);
                Assert.Equal(1, service.DefaultInstances);
                Assert.Equal(0, service.MinimumInstances);
                Assert.Null(service.MaximumInstances);
            });
    }

    [Fact]
    public void IsActiveAt_UsesInclusiveStartAndExpireBounds()
    {
        var now = DomainTestData.UtcNow;

        Assert.True(DomainTestData.CreatePackage().IsActiveAt(now));
        Assert.False(DomainTestData.CreatePackage(start: now.AddTicks(1)).IsActiveAt(now));
        Assert.True(DomainTestData.CreatePackage(start: now).IsActiveAt(now));
        Assert.False(DomainTestData.CreatePackage(expire: now.AddTicks(-1)).IsActiveAt(now));
        Assert.True(DomainTestData.CreatePackage(expire: now).IsActiveAt(now));
    }

    [Fact]
    public void Update_ChangesPackageValuesAndReplacesExistingChildren()
    {
        var createdAt = DomainTestData.UtcNow;
        var updatedAt = createdAt.AddDays(1);
        var package = Package.Create(
            "Starter",
            packageCategoryId: 1,
            description: null,
            start: null,
            expire: null,
            isQuantityAllowed: false,
            [
                DomainTestData.FrequencyDefinition(name: "Monthly", frequency: 30),
                DomainTestData.FrequencyDefinition(name: "Yearly", frequency: 365)
            ],
            [
                DomainTestData.ServiceDefinition(serviceId: 101),
                DomainTestData.ServiceDefinition(serviceId: 102)
            ],
            createdAt);

        var monthly = package.Frequencies.First(frequency => frequency.Name == "Monthly");
        var yearly = package.Frequencies.First(frequency => frequency.Name == "Yearly");
        var service101 = package.Services.First(service => service.ServiceId == 101);
        var service102 = package.Services.First(service => service.ServiceId == 102);
        DomainTestData.SetEntityId(monthly, 10);
        DomainTestData.SetEntityId(yearly, 11);
        DomainTestData.SetEntityId(service101, 20);
        DomainTestData.SetEntityId(service102, 21);

        package.Update(
            "  Professional  ",
            packageCategoryId: 2,
            description: "  Updated description  ",
            start: null,
            expire: null,
            isQuantityAllowed: true,
            [
                DomainTestData.FrequencyDefinition(id: 10, name: "Quarterly", frequency: 90, isActive: false),
                DomainTestData.FrequencyDefinition(name: "Weekly", frequency: 7)
            ],
            [
                DomainTestData.ServiceDefinition(id: 20, serviceId: 103, defaultInstances: 2, minimumInstances: 1, maximumInstances: 5),
                DomainTestData.ServiceDefinition(serviceId: 104, defaultInstances: 1, minimumInstances: 0, maximumInstances: null)
            ],
            updatedAt,
            fullPeriod: true,
            postPaid: true);

        Assert.Equal("Professional", package.Name);
        Assert.Equal(2, package.PackageCategoryId);
        Assert.Equal("Updated description", package.Description);
        Assert.True(package.IsQuantityAllowed);
        Assert.True(package.FullPeriod);
        Assert.True(package.PostPaid);
        Assert.Empty(package.DomainEvents);

        Assert.Equal(2, package.Frequencies.Count);
        Assert.DoesNotContain(package.Frequencies, frequency => frequency.Id == 11);
        var quarterly = Assert.Single(package.Frequencies, frequency => frequency.Id == 10);
        Assert.Equal("Quarterly", quarterly.Name);
        Assert.Equal(90, quarterly.Frequency);
        Assert.False(quarterly.IsActive);
        Assert.Equal(createdAt, quarterly.Created);
        var weekly = Assert.Single(package.Frequencies, frequency => frequency.Name == "Weekly");
        Assert.Equal(updatedAt, weekly.Created);

        Assert.Equal(2, package.Services.Count);
        Assert.DoesNotContain(package.Services, service => service.Id == 21);
        var updatedService = Assert.Single(package.Services, service => service.Id == 20);
        Assert.Equal(103, updatedService.ServiceId);
        Assert.Equal(2, updatedService.DefaultInstances);
        Assert.Equal(1, updatedService.MinimumInstances);
        Assert.Equal(5, updatedService.MaximumInstances);
        Assert.Contains(package.Services, service => service.ServiceId == 104 && service.MaximumInstances is null);
    }

    [Fact]
    public void Update_UpdatesExistingFrequencyByName_WhenFrequencyIdIsMissing()
    {
        var createdAt = DomainTestData.UtcNow;
        var updatedAt = createdAt.AddDays(1);
        var package = DomainTestData.CreatePackage(utcNow: createdAt);
        var monthly = Assert.Single(package.Frequencies);
        var service = Assert.Single(package.Services);
        DomainTestData.SetEntityId(monthly, 10);
        DomainTestData.SetEntityId(service, 20);

        package.Update(
            "Starter",
            packageCategoryId: 1,
            description: "Base package",
            start: null,
            expire: null,
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition(name: " monthly ", frequency: 60, isActive: false)],
            [DomainTestData.ServiceDefinition(id: 20, serviceId: 101)],
            updatedAt);

        var updatedFrequency = Assert.Single(package.Frequencies);
        Assert.Same(monthly, updatedFrequency);
        Assert.Equal(10, updatedFrequency.Id);
        Assert.Equal("monthly", updatedFrequency.Name);
        Assert.Equal(60, updatedFrequency.Frequency);
        Assert.False(updatedFrequency.IsActive);
        Assert.Equal(createdAt, updatedFrequency.Created);
    }

    [Fact]
    public void Update_UpdatesExistingServiceByServiceId_WhenPackageServiceIdIsMissing()
    {
        var package = DomainTestData.CreatePackage();
        var frequency = Assert.Single(package.Frequencies);
        var service = Assert.Single(package.Services);
        DomainTestData.SetEntityId(frequency, 10);
        DomainTestData.SetEntityId(service, 20);

        package.Update(
            "Starter",
            packageCategoryId: 1,
            description: "Base package",
            start: null,
            expire: null,
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition(id: 10)],
            [DomainTestData.ServiceDefinition(serviceId: 101, defaultInstances: 3, minimumInstances: 2, maximumInstances: 5)],
            DomainTestData.UtcNow);

        var updatedService = Assert.Single(package.Services);
        Assert.Same(service, updatedService);
        Assert.Equal(20, updatedService.Id);
        Assert.Equal(101, updatedService.ServiceId);
        Assert.Equal(3, updatedService.DefaultInstances);
        Assert.Equal(2, updatedService.MinimumInstances);
        Assert.Equal(5, updatedService.MaximumInstances);
    }

    [Fact]
    public void Update_RaisesActivationChangedDomainEvent_WhenPackageBecomesInactive()
    {
        var now = DomainTestData.UtcNow;
        var package = DomainTestData.CreatePackage(start: now.AddDays(-1), expire: now.AddDays(1));
        DomainTestData.SetEntityId(package, 42);

        package.Update(
            "Starter",
            packageCategoryId: 1,
            description: "Base package",
            start: now.AddDays(1),
            expire: now.AddDays(2),
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition()],
            [DomainTestData.ServiceDefinition()],
            now);

        var domainEvent = Assert.IsType<PackageActivationChangedDomainEvent>(Assert.Single(package.DomainEvents));
        Assert.Equal(42, domainEvent.PackageId);
        Assert.False(domainEvent.IsActive);
        Assert.Equal(now, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void Update_RaisesActivationChangedDomainEvent_WhenPackageBecomesActive()
    {
        var now = DomainTestData.UtcNow;
        var package = DomainTestData.CreatePackage(start: now.AddDays(1), expire: now.AddDays(2));
        DomainTestData.SetEntityId(package, 43);

        package.Update(
            "Starter",
            packageCategoryId: 1,
            description: "Base package",
            start: now.AddDays(-1),
            expire: now.AddDays(1),
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition()],
            [DomainTestData.ServiceDefinition()],
            now);

        var domainEvent = Assert.IsType<PackageActivationChangedDomainEvent>(Assert.Single(package.DomainEvents));
        Assert.Equal(43, domainEvent.PackageId);
        Assert.True(domainEvent.IsActive);
        Assert.Equal(now, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void Update_DoesNotRaiseActivationChangedDomainEvent_WhenActiveStateDoesNotChange()
    {
        var now = DomainTestData.UtcNow;
        var package = DomainTestData.CreatePackage(start: now.AddDays(-2), expire: now.AddDays(2));

        package.Update(
            "Starter Plus",
            packageCategoryId: 1,
            description: "Still active",
            start: now.AddDays(-1),
            expire: now.AddDays(1),
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition()],
            [DomainTestData.ServiceDefinition()],
            now);

        Assert.Empty(package.DomainEvents);
    }

    [Fact]
    public void Create_Throws_WhenStartIsAfterExpire()
    {
        var now = DomainTestData.UtcNow;

        var exception = Assert.Throws<DomainException>(() =>
            DomainTestData.CreatePackage(start: now.AddDays(1), expire: now));

        Assert.Equal("Package start date cannot be later than expire date.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsMissing(string? name)
    {
        Assert.Throws<DomainException>(() =>
            Package.Create(
                name!,
                packageCategoryId: 1,
                description: null,
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition()],
                [DomainTestData.ServiceDefinition()],
                DomainTestData.UtcNow));
    }

    [Fact]
    public void Create_Throws_WhenDescriptionExceedsMaximumLength()
    {
        Assert.Throws<DomainException>(() =>
            Package.Create(
                "Starter",
                packageCategoryId: 1,
                description: new string('a', 2001),
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition()],
                [DomainTestData.ServiceDefinition()],
                DomainTestData.UtcNow));
    }

    [Fact]
    public void Update_Throws_WhenStartIsAfterExpire()
    {
        var now = DomainTestData.UtcNow;
        var package = DomainTestData.CreatePackage();

        var exception = Assert.Throws<DomainException>(() =>
            package.Update(
                "Starter",
                packageCategoryId: 1,
                description: null,
                start: now.AddDays(1),
                expire: now,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition()],
                [DomainTestData.ServiceDefinition()],
                now));

        Assert.Equal("Package start date cannot be later than expire date.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Throws_WhenPackageCategoryIdIsNotGreaterThanZero(int packageCategoryId)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Package.Create(
                "Starter",
                packageCategoryId,
                description: null,
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition()],
                [DomainTestData.ServiceDefinition()],
                DomainTestData.UtcNow));

        Assert.Equal("PackageCategoryId must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenPackageCategoryIdIsNotDefined()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Package.Create(
                "Starter",
                packageCategoryId: 99,
                description: null,
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition()],
                [DomainTestData.ServiceDefinition()],
                DomainTestData.UtcNow));

        Assert.Equal("Package category 99 is not supported.", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenFrequencyNamesAreDuplicatedAfterTrimAndCaseNormalization()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Package.Create(
                "Starter",
                packageCategoryId: 1,
                description: null,
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [
                    DomainTestData.FrequencyDefinition(name: " Monthly "),
                    DomainTestData.FrequencyDefinition(name: "monthly")
                ],
                [DomainTestData.ServiceDefinition()],
                DomainTestData.UtcNow));

        Assert.Equal("Package frequency name 'Monthly' is duplicated.", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenServiceIdsAreDuplicated()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Package.Create(
                "Starter",
                packageCategoryId: 1,
                description: null,
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition()],
                [
                    DomainTestData.ServiceDefinition(serviceId: 101),
                    DomainTestData.ServiceDefinition(serviceId: 101)
                ],
                DomainTestData.UtcNow));

        Assert.Equal("Service 101 is duplicated in this package.", exception.Message);
    }

    [Fact]
    public void Update_Throws_WhenFrequencyIdDoesNotBelongToPackage()
    {
        var package = DomainTestData.CreatePackage();

        var exception = Assert.Throws<DomainException>(() =>
            package.Update(
                "Starter",
                packageCategoryId: 1,
                description: null,
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition(id: 999, name: "Weekly", frequency: 7)],
                [DomainTestData.ServiceDefinition()],
                DomainTestData.UtcNow));

        Assert.Equal("Package frequency 999 does not belong to this package.", exception.Message);
    }

    [Fact]
    public void Update_AllowsRequestedFrequencyToUseNameFromOmittedExistingFrequency()
    {
        var package = Package.Create(
            "Starter",
            packageCategoryId: 1,
            description: null,
            start: null,
            expire: null,
            isQuantityAllowed: true,
            [
                DomainTestData.FrequencyDefinition(name: "Monthly", frequency: 30),
                DomainTestData.FrequencyDefinition(name: "Weekly", frequency: 7)
            ],
            [DomainTestData.ServiceDefinition()],
            DomainTestData.UtcNow);
        DomainTestData.SetEntityId(package.Frequencies.First(frequency => frequency.Name == "Monthly"), 10);
        DomainTestData.SetEntityId(package.Frequencies.First(frequency => frequency.Name == "Weekly"), 11);

        package.Update(
            "Starter",
            packageCategoryId: 1,
            description: null,
            start: null,
            expire: null,
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition(id: 10, name: " weekly ", frequency: 14)],
            [DomainTestData.ServiceDefinition()],
            DomainTestData.UtcNow);

        var updatedFrequency = Assert.Single(package.Frequencies);
        Assert.Equal(10, updatedFrequency.Id);
        Assert.Equal("weekly", updatedFrequency.Name);
        Assert.Equal(14, updatedFrequency.Frequency);
    }

    [Fact]
    public void Update_Throws_WhenServiceIdDoesNotBelongToPackage()
    {
        var package = DomainTestData.CreatePackage();

        var exception = Assert.Throws<DomainException>(() =>
            package.Update(
                "Starter",
                packageCategoryId: 1,
                description: null,
                start: null,
                expire: null,
                isQuantityAllowed: true,
                [DomainTestData.FrequencyDefinition()],
                [DomainTestData.ServiceDefinition(id: 999, serviceId: 102)],
                DomainTestData.UtcNow));

        Assert.Equal("Package service 999 does not belong to this package.", exception.Message);
    }

    [Fact]
    public void Update_AllowsRequestedServiceToUseServiceIdFromOmittedExistingPackageService()
    {
        var package = Package.Create(
            "Starter",
            packageCategoryId: 1,
            description: null,
            start: null,
            expire: null,
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition()],
            [
                DomainTestData.ServiceDefinition(serviceId: 101),
                DomainTestData.ServiceDefinition(serviceId: 102)
            ],
            DomainTestData.UtcNow);
        DomainTestData.SetEntityId(package.Services.First(service => service.ServiceId == 101), 20);
        DomainTestData.SetEntityId(package.Services.First(service => service.ServiceId == 102), 21);

        package.Update(
            "Starter",
            packageCategoryId: 1,
            description: null,
            start: null,
            expire: null,
            isQuantityAllowed: true,
            [DomainTestData.FrequencyDefinition()],
            [DomainTestData.ServiceDefinition(id: 20, serviceId: 102)],
            DomainTestData.UtcNow);

        var updatedService = Assert.Single(package.Services);
        Assert.Equal(20, updatedService.Id);
        Assert.Equal(102, updatedService.ServiceId);
    }
}
