using MediatR;

namespace FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;

public sealed record AddPhysicalMeasureCommand(
    Guid ClientProfileId,
    decimal Weight,
    decimal BodyFatPercentage,
    decimal MuscleMass,
    decimal Waist,
    decimal Chest,
    decimal Arms
) : IRequest;