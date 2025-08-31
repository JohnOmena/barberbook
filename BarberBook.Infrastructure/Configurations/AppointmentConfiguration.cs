using BarberBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberBook.Infrastructure.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ProfessionalId).IsRequired();
        builder.Property(x => x.ServiceId).IsRequired();

        // timestamptz columns
        builder.Property(x => x.StartsAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.EndsAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // Status as smallint
        builder.Property(x => x.Status)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.ClientName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ClientContact)
            .IsRequired()
            .HasMaxLength(200);

        // indexes
        builder.HasIndex(x => x.StartsAt);
        builder.HasIndex(x => new { x.TenantId, x.ProfessionalId, x.StartsAt }).IsUnique();
    }
}
