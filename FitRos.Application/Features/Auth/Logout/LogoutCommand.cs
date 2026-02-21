using MediatR;

namespace FitRos.Application.Features.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;