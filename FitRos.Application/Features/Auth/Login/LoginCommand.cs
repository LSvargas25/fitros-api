using FitRos.Application.Common.Security;
using MediatR;

namespace FitRos.Application.Features.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResponse>, IAllowAnonymousRequest;

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    int Role,
    string AccessToken,
    string RefreshToken
);