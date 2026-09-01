using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class WorkoutSessionConfiguration
    : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.RoutineId)
            .IsRequired();

        builder.Property(x => x.RoutineNameSnapshot)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.RoutineVersion)
            .IsRequired();

        builder.Property(x => x.ScheduledDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
         
        builder.Ignore(x => x.Sets);
         
        builder.HasMany(typeof(ExerciseSet), "_sets")
            .WithOne()
            .HasForeignKey("WorkoutSessionId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
