 
using FitRos.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class PhysicalMeasureConfiguration : IEntityTypeConfiguration<PhysicalMeasure>
{
    public void Configure(EntityTypeBuilder<PhysicalMeasure> builder)
    {
        builder.ToTable("PhysicalMeasures");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ClientProfileId)
            .IsRequired();

        builder.Property(x => x.Weight)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.BodyFatPercentage)
            .HasPrecision(5, 2);

        builder.Property(x => x.MuscleMass)
            .HasPrecision(10, 2);

        builder.Property(x => x.Waist)
            .HasPrecision(10, 2);

        builder.Property(x => x.Chest)
            .HasPrecision(10, 2);

        builder.Property(x => x.Arms)
            .HasPrecision(10, 2);

        builder.Property(x => x.RecordedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.ClientProfileId, x.RecordedAt });
    }
}