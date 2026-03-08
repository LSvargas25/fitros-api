using FitRos.Application.Common.Security;
using MediatR;

namespace FitRos.Application.Features.Auth.Refresh;

public sealed record RefreshCommand(
    string RefreshToken
) : IRequest<RefreshResponse>, IAllowAnonymousRequest;

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken
);