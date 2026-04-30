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

        builder.HasData(new
        {
            Id = 1,
            Name = "Default",
            SortOrder = 1,
            Visible = true
        });
    }
}
