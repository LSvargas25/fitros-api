using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class ExerciseSetConfiguration
    : IEntityTypeConfiguration<ExerciseSet>
{
    public void Configure(EntityTypeBuilder<ExerciseSet> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ExerciseId)
            .IsRequired();

        builder.Property(x => x.SetNumber)
            .IsRequired();

        builder.Property(x => x.RepsAchieved)
            .IsRequired();

        builder.Property(x => x.WeightUsed)
            .HasPrecision(10, 2) // importante para decimal en PostgreSQL
            .IsRequired();

        // Shadow FK
        builder.Property<Guid>("WorkoutSessionId");
    }
}
