using FitRos.Application.Common.Security;
using MediatR;

namespace FitRos.Application.Features.Auth.VerifyEmail;

public sealed record VerifyEmailCommand(
    string Email,
    string Code
) : IRequest, IAllowAnonymousRequest;
