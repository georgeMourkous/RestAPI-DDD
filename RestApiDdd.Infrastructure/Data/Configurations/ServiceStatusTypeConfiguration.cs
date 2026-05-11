using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Infrastructure.Data.Configurations;

internal sealed class ServiceStatusTypeConfiguration : IEntityTypeConfiguration<ServiceStatusType>
{
    public void Configure(EntityTypeBuilder<ServiceStatusType> builder)
    {
        builder.ToTable("ServiceStatusType");

        builder.HasKey(statusType => statusType.Id);

        builder.Property(statusType => statusType.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(statusType => statusType.Name)
            .IsUnique();

        builder.Property(statusType => statusType.SortOrder)
            .IsRequired();

        builder.Property(statusType => statusType.TokenName)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(statusType => statusType.TokenName)
            .IsUnique();

        builder.HasData(
            new
            {
                Id = 1,
                Name = "Recurring Charge",
                SortOrder = 1,
                TokenName = "mrc"
            },
            new
            {
                Id = 2,
                Name = "Non-recurring Charge",
                SortOrder = 2,
                TokenName = "nrc"
            },
            new
            {
                Id = 3,
                Name = "Fee",
                SortOrder = 3,
                TokenName = "fee"
            });
    }
}
