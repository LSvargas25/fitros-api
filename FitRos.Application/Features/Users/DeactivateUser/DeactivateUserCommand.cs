using MediatR;

namespace FitRos.Application.Features.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest;