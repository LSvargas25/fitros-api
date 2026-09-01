using MediatR;

namespace FitRos.Application.Features.ClientProfiles.GenerateKpiSnapshot;

public sealed record GenerateClientKpiSnapshotCommand(
    Guid ClientProfileId) : IRequest<GenerateClientKpiSnapshotResponse>;

public sealed record GenerateClientKpiSnapshotResponse(
    Guid Id,
    Guid PhysicalMeasureId,
    decimal? WeightDelta,
    decimal? BodyFatDelta,
    decimal? WaistDelta);
