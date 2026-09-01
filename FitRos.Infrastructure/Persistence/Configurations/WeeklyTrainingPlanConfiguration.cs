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
            .IsRequired(false);

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

        // Backs the "one active plan per client" rule at the DB level too -
        // ActivatePlanHandler archives every other active plan for the
        // client before activating this one, but two concurrent activation
        // requests can still both pass that check before either commits.
        builder.HasIndex(x => x.ClientProfileId)
            .IsUnique()
            .HasFilter("\"Status\" = 2")
            .HasDatabaseName("IX_WeeklyTrainingPlans_ClientProfileId_ActiveOnly");

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
