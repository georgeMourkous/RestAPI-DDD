using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Infrastructure.Data.Configurations;

internal sealed class PackageCategoryConfiguration : IEntityTypeConfiguration<PackageCategory>
{
    public void Configure(EntityTypeBuilder<PackageCategory> builder)
    {
        builder.ToTable("PackageCategory");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.Property(category => category.SortOrder)
            .IsRequired();

        builder.Property(category => category.Visible)
            .IsRequired();

        builder.HasData(
            new
            {
                Id = (int)PackageCategoryType.SharePlan,
                Name = "Share Plan",
                SortOrder = 1,
                Visible = false
            },
            new
            {
                Id = (int)PackageCategoryType.Default,
                Name = "Default",
                SortOrder = 2,
                Visible = true
            },
            new
            {
                Id = (int)PackageCategoryType.BillingActivation,
                Name = "Billing Activation",
                SortOrder = 3,
                Visible = true
            },
            new
            {
                Id = (int)PackageCategoryType.SharePlanAddOn,
                Name = "Share Plan Add-on",
                SortOrder = 4,
                Visible = true
            },
            new
            {
                Id = (int)PackageCategoryType.OneTimeBilling,
                Name = "One Time Billing",
                SortOrder = 5,
                Visible = true
            },
            new
            {
                Id = (int)PackageCategoryType.GlobalAddOn,
                Name = "Global Add-on",
                SortOrder = 6,
                Visible = false
            });
    }
}
