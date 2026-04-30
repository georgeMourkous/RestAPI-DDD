using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceAggregate = RestApiDdd.Domain.Entities.Service;

namespace RestApiDdd.Infrastructure.Data.Configurations;

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<ServiceAggregate>
{
    public void Configure(EntityTypeBuilder<ServiceAggregate> builder)
    {
        builder.ToTable("Service");

        builder.HasKey(service => service.Id);

        builder.Ignore(service => service.DomainEvents);

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
            Description = "Default seeded service value."
        });
    }
}
