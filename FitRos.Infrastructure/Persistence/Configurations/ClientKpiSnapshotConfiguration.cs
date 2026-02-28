using FitRos.Domain.Entities.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class ClientKpiSnapshotConfiguration : IEntityTypeConfiguration<ClientKpiSnapshot>
{
    public void Configure(EntityTypeBuilder<ClientKpiSnapshot> builder)
    {
        builder.ToTable("ClientKpiSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ClientProfileId)
            .IsRequired();

        builder.Property(x => x.PhysicalMeasureId)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}