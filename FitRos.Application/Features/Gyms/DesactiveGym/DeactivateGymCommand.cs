using MediatR;

namespace FitRos.Application.Features.Gyms.DeactivateGym;

public record DeactivateGymCommand(
    Guid GymId
) : IRequest;