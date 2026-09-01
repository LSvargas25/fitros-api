using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Coach.GetCoaches;

public sealed record GetCoachesQuery : IRequest<List<CoachListItemDto>>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin };
}
