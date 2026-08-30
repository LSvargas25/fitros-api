using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ClientProfileConfiguration : IEntityTypeConfiguration<ClientProfile>
{
    public void Configure(EntityTypeBuilder<ClientProfile> builder)
    {
        builder.ToTable("ClientProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CoachId)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        // The domain constructor always sets Kind, so no store-side default is
        // needed for writes; existing rows are backfilled to 0 (GymClient) by
        // the AddColumn default in the migration.
        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.ModifiedBy)
            .IsRequired(false);

        builder.Property(x => x.ModifiedAt)
            .IsRequired(false);

        builder.Property(x => x.DeactivatedAt)
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Measures)
            .WithOne()
            .HasForeignKey(x => x.ClientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(ClientProfile.Measures))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

       
    }
}