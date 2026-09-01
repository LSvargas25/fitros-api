using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.Coach.GetCoachById;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Coach.UpdateCoach;

public sealed record UpdateCoachCommand(
    Guid CoachId,
    string? FirstName,
    string? LastName,
    string? Email) : IRequest<CoachDetailDto>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin };
}
