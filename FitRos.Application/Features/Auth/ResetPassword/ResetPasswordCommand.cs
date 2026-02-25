using MediatR;

namespace FitRos.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : IRequest;