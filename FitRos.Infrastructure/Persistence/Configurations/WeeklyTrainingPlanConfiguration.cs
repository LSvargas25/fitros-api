using FitRos.Domain.Entities.WeeklyTraining;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class WeeklyTrainingPlanConfiguration : IEntityTypeConfiguration<WeeklyTrainingPlan>
{
    public void Configure(EntityTypeBuilder<WeeklyTrainingPlan> builder)
    {
        builder.ToTable("WeeklyTrainingPlans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ClientProfileId)
            .IsRequired();

        builder.Property(x => x.CoachId)
            .IsRequired();

        builder.Property(x => x.GymId)
            .IsRequired(false);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.ClientProfileId);
        builder.HasIndex(x => new { x.ClientProfileId, x.Status });

        builder
            .HasMany(p => p.Days)
            .WithOne()
            .HasForeignKey(d => d.WeeklyTrainingPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(p => p.Days)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
