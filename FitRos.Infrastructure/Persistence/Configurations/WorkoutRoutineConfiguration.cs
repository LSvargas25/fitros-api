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

        // 🔥 IMPORTANTE: Id lo genera el dominio, no la base
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.NormalizedName)
            .IsUnique();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired()
            .ValueGeneratedNever();

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
