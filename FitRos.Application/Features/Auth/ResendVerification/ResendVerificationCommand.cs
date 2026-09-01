using FitRos.Application.Common.Security;
using MediatR;

namespace FitRos.Application.Features.Auth.ResendVerification;

public sealed record ResendVerificationCommand(
    string Email
) : IRequest, IAllowAnonymousRequest;
