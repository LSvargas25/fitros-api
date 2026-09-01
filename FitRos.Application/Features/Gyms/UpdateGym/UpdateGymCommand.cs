using MediatR;

namespace FitRos.Application.Features.Gyms.UpdateGym;

public record UpdateGymCommand(
    Guid GymId,
    string Name,
    string Address,
    string PhoneNumber
) : IRequest;