using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Gyms.GetAllGyms;

public sealed record GetAllGymsQuery : IRequest<List<GymListItemDto>>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
