using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Infrastructure.Data.Configurations;

internal sealed class PackageFrequencyConfiguration : IEntityTypeConfiguration<PackageFrequency>
{
    public void Configure(EntityTypeBuilder<PackageFrequency> builder)
    {
        builder.ToTable("PackageFrequency");

        builder.HasKey(frequency => frequency.Id);

        builder.Property(frequency => frequency.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(frequency => frequency.Frequency)
            .IsRequired();

        builder.Property(frequency => frequency.IsActive)
            .IsRequired();

        builder.Property(frequency => frequency.Created)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(frequency => new { frequency.PackageId, frequency.Name })
            .IsUnique();
    }
}
