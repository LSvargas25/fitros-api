using MediatR;

namespace FitRos.Application.Features.Auth.ForgotPassword
{

    public sealed record ForgotPasswordCommand(string Email) : IRequest;
}
