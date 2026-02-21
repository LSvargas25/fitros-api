using MediatR;

namespace FitRos.Application.Features.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<RefreshResponse>;

public sealed record RefreshResponse(string AccessToken, string RefreshToken);