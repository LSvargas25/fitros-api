namespace FitRos.Domain.Entities.Reports;

public sealed class ClientProgressReportSnapshot
{
    public Guid Id { get; private set; }

    public Guid ClientProfileId { get; private set; }

    public Guid PhysicalMeasureId { get; private set; }

    public string ReportJson { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    private ClientProgressReportSnapshot() { }

    private ClientProgressReportSnapshot(
        Guid id,
        Guid clientProfileId,
        Guid physicalMeasureId,
        string reportJson)
    {
        Id = id;
        ClientProfileId = clientProfileId;
        PhysicalMeasureId = physicalMeasureId;
        ReportJson = reportJson;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static ClientProgressReportSnapshot Create(
        Guid clientProfileId,
        Guid physicalMeasureId,
        string reportJson)
        => new(Guid.NewGuid(), clientProfileId, physicalMeasureId, reportJson);
}