using MediatR;

namespace FitRos.Application.Features.Gyms.SoftDeleteGym;

public sealed record SoftDeleteGymCommand(Guid GymId) : IRequest;