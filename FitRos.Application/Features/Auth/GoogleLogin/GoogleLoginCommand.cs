using FitRos.Application.Common.Security;
using MediatR;

namespace FitRos.Application.Features.Auth.GoogleLogin;

public sealed record GoogleLoginCommand(
    string IdToken
) : IRequest<GoogleLoginResponse>, IAllowAnonymousRequest;

public sealed record GoogleLoginResponse(
    Guid UserId,
    string Email,
    int Role,
    string AccessToken,
    string RefreshToken
);
