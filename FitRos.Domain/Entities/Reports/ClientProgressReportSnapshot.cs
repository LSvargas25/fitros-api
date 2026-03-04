using FitRos.Domain.Common;

namespace FitRos.Domain.Entities.Reports;

public sealed class ClientProgressReportSnapshot : ITenantEntity
{
    public Guid Id { get; private set; }

    public Guid ClientProfileId { get; private set; }

    public Guid PhysicalMeasureId { get; private set; }

    public string ReportJson { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? GymId { get; private set; }

    private ClientProgressReportSnapshot() { }

    private ClientProgressReportSnapshot(
       Guid gymId,
       Guid id,
       Guid clientProfileId,
       Guid physicalMeasureId,
       string reportJson)
    {
        GymId = gymId;

        Id = id;
        ClientProfileId = clientProfileId;
        PhysicalMeasureId = physicalMeasureId;
        ReportJson = reportJson;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static ClientProgressReportSnapshot Create(
     Guid gymId,
     Guid clientProfileId,
     Guid physicalMeasureId,
     string reportJson)
     => new(gymId, Guid.NewGuid(), clientProfileId, physicalMeasureId, reportJson);
}