using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiDdd.Domain.Entities;

namespace RestApiDdd.Infrastructure.Data.Configurations;

internal sealed class ServiceLookupConfiguration : IEntityTypeConfiguration<ServiceLookup>
{
    public void Configure(EntityTypeBuilder<ServiceLookup> builder)
    {
        builder.ToTable("Service");

        builder.HasKey(service => service.Id);

        builder.Property(service => service.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(service => service.Name)
            .IsUnique();

        builder.Property(service => service.Description)
            .HasMaxLength(2000);

        builder.HasData(new
        {
            Id = 1,
            Name = "Core Service",
            Description = "Default seeded service lookup value."
        });
    }
}
