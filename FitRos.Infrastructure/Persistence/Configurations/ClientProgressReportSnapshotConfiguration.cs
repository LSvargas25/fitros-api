using FitRos.Domain.Entities.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitRos.Infrastructure.Persistence.Configurations;

public class ClientProgressReportSnapshotConfiguration
    : IEntityTypeConfiguration<ClientProgressReportSnapshot>
{
    public void Configure(EntityTypeBuilder<ClientProgressReportSnapshot> builder)
    {
        builder.ToTable("ClientProgressReportSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ClientProfileId)
            .IsRequired();

        builder.Property(x => x.PhysicalMeasureId)
            .IsRequired();

        builder.Property(x => x.ReportJson)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}