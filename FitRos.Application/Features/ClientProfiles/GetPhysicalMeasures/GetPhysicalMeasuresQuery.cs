using MediatR;

namespace FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;

public sealed record GetPhysicalMeasuresQuery(Guid ClientProfileId) : IRequest<List<PhysicalMeasureHistoryDto>>;

public sealed record PhysicalMeasureHistoryDto(
    Guid Id,
    decimal Weight,
    decimal BodyFatPercentage,
    decimal MuscleMass,
    decimal Waist,
    decimal Chest,
    decimal Arms,
    DateTime RecordedAt);
