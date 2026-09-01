using FitRos.Domain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    public void Configure(EntityTypeBuilder<Food> builder)
    {
        builder.ToTable("Foods");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CaloriesPer100g)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.ProteinPer100g)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.CarbsPer100g)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.FatPer100g)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.ServingSizeGrams)
            .HasPrecision(10, 2)
            .IsRequired(false);

        builder.Property(x => x.IsArchived)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.GymId)
            .IsRequired(false);

        builder.HasIndex(x => new { x.GymId, x.NormalizedName })
            .IsUnique();

        builder.HasIndex(x => x.Category);
    }
}
