using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Coach.GetCoachById;

public sealed record GetCoachByIdQuery(Guid CoachId) : IRequest<CoachDetailDto?>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin };
}
