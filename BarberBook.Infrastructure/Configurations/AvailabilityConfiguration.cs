using BarberBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberBook.Infrastructure.Configurations;

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("availabilities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ProfessionalId).IsRequired();

        builder.Property(x => x.Weekday)
            .IsRequired();

        // Start/End are TimeSpan; PostgreSQL will map to 'interval' by default (OK for time-of-day usage)
        builder.Property(x => x.Start).IsRequired();
        builder.Property(x => x.End).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ProfessionalId, x.Weekday });
    }
}

