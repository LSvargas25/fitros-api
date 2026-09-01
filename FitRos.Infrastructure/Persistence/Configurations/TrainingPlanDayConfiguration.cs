using FitRos.Domain.Entities.WeeklyTraining;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class TrainingPlanDayConfiguration : IEntityTypeConfiguration<TrainingPlanDay>
{
    public void Configure(EntityTypeBuilder<TrainingPlanDay> builder)
    {
        builder.ToTable("TrainingPlanDays");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WeeklyTrainingPlanId)
            .IsRequired();

        builder.Property(x => x.Day)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.WorkoutRoutineId)
            .IsRequired();

        builder.Property(x => x.Notes)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.WeeklyTrainingPlanId, x.Day })
            .IsUnique();
    }
}
