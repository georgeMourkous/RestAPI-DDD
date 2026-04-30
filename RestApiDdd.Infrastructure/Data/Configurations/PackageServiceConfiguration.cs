using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Infrastructure.Data.Configurations;

internal sealed class PackageServiceConfiguration : IEntityTypeConfiguration<PackageService>
{
    public void Configure(EntityTypeBuilder<PackageService> builder)
    {
        builder.ToTable("PackageService");

        builder.HasKey(packageService => packageService.Id);

        builder.Property(packageService => packageService.DefaultInstances)
            .IsRequired();

        builder.Property(packageService => packageService.MinimumInstances)
            .IsRequired();

        builder.Property(packageService => packageService.MaximumInstances);

        builder.HasOne(packageService => packageService.Service)
            .WithMany()
            .HasForeignKey(packageService => packageService.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(packageService => new { packageService.PackageId, packageService.ServiceId })
            .IsUnique();
    }
}
