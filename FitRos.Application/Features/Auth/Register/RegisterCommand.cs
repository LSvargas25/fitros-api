using FitRos.Application.Common.Security;
using MediatR;

namespace FitRos.Application.Features.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
) : IRequest<RegisterResponse>, IAllowAnonymousRequest;

public sealed record RegisterResponse(
    Guid UserId,
    string Email
);
