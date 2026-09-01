using MediatR;

namespace FitRos.Application.Features.Gyms.ActivateGym;

public record ActivateGymCommand(
    Guid GymId
) : IRequest;