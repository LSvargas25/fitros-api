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
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder
            .HasMany(x => x.Measures)
            .WithOne()
            .HasForeignKey(x => x.ClientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(ClientProfile.Measures))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.RowVersion)
     .IsRequired()
     .IsConcurrencyToken()
     .ValueGeneratedOnAddOrUpdate();
    }
}