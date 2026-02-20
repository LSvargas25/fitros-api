using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class WorkoutRoutineConfiguration
    : IEntityTypeConfiguration<WorkoutRoutine>
{
    public void Configure(EntityTypeBuilder<WorkoutRoutine> builder)
    {
        builder.ToTable("WorkoutRoutines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(200);

        // Unique only for the base routine (Version = 1).
        // This allows multiple versions to share the same NormalizedName.
        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasFilter("\"Version\" = 1");

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired()
            .ValueGeneratedNever();

        // Version group identifier (ties multiple versions together)
        builder.Property(x => x.RoutineGroupId)
            .IsRequired()
            .ValueGeneratedNever();

        // Index for fast version queries (latest, list versions)
        builder.HasIndex(x => new { x.RoutineGroupId, x.Version });

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder
            .HasMany(r => r.Exercises)
            .WithOne(e => e.WorkoutRoutine)
            .HasForeignKey(e => e.WorkoutRoutineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(r => r.Exercises)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}