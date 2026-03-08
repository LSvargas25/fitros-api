using FitRos.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.GymId)
            .IsRequired(false);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ReferenceId)
            .IsRequired(false);

        builder.Property(x => x.IsRead)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsRead });
    }
}
