using FitRos.Domain.Common;

namespace FitRos.Domain.Entities.Analytics;

public sealed class ClientKpiSnapshot : ITenantEntity
{
    public Guid Id { get; private set; }

    public Guid ClientProfileId { get; private set; }

    public Guid PhysicalMeasureId { get; private set; }

    public decimal? WeightDelta { get; private set; }

    public decimal? BodyFatDelta { get; private set; }

    public decimal? WaistDelta { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? GymId { get; private set; }


    private ClientKpiSnapshot() { }

    private ClientKpiSnapshot(
        Guid id,
        Guid clientProfileId,
        Guid physicalMeasureId,
        decimal? weightDelta,
        decimal? bodyFatDelta,
        decimal? waistDelta)
    {
        Id = id;
        ClientProfileId = clientProfileId;
        PhysicalMeasureId = physicalMeasureId;
        WeightDelta = weightDelta;
        BodyFatDelta = bodyFatDelta;
        WaistDelta = waistDelta;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static ClientKpiSnapshot Create(
        Guid clientProfileId,
        Guid physicalMeasureId,
        decimal? weightDelta,
        decimal? bodyFatDelta,
        decimal? waistDelta)
        => new(Guid.NewGuid(), clientProfileId, physicalMeasureId, weightDelta, bodyFatDelta, waistDelta);
}