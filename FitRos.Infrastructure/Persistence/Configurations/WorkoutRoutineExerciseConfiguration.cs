using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class WorkoutRoutineExerciseConfiguration
    : IEntityTypeConfiguration<WorkoutRoutineExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutRoutineExercise> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExerciseId)
            .IsRequired();

        builder.Property(x => x.Order)
            .IsRequired();

        builder.Property(x => x.SuggestedSets)
            .IsRequired();

        builder.Property(x => x.SuggestedReps)
            .IsRequired();

        builder.Property(x => x.SuggestedRestSeconds)
            .IsRequired();

        // Shadow property FK (porque no existe en el dominio)
        builder.Property<Guid>("WorkoutRoutineId");
    }
}
