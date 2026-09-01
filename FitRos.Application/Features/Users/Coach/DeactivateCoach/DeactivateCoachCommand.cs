using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Coach.DeactivateCoach;

public sealed record DeactivateCoachCommand(Guid CoachId) : IRequest, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin };
}
