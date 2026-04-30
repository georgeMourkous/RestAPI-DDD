using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Infrastructure.Data.Configurations;

internal sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("Package");

        builder.HasKey(package => package.Id);

        builder.Ignore(package => package.DomainEvents);

        builder.Property(package => package.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(package => package.Name)
            .IsUnique();

        builder.Property(package => package.Description)
            .HasMaxLength(2000);

        builder.Property(package => package.Created)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(package => package.Start);

        builder.Property(package => package.Expire);

        builder.Property(package => package.IsQuantityAllowed)
            .IsRequired();

        builder.HasOne(package => package.PackageCategory)
            .WithMany()
            .HasForeignKey(package => package.PackageCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(package => package.Frequencies)
            .WithOne(frequency => frequency.Package)
            .HasForeignKey(frequency => frequency.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(package => package.Services)
            .WithOne(service => service.Package)
            .HasForeignKey(service => service.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(package => package.Frequencies)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(package => package.Services)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
