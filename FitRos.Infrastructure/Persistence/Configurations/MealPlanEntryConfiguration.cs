using FitRos.Domain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class MealPlanEntryConfiguration : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable("MealPlanEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.MealPlanId)
            .IsRequired();

        builder.Property(x => x.Day)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Meal)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.FoodId)
            .IsRequired();

        builder.Property(x => x.QuantityGrams)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasIndex(x => new { x.MealPlanId, x.Day, x.Meal, x.FoodId })
            .IsUnique();

        builder.HasIndex(x => x.FoodId);
    }
}
