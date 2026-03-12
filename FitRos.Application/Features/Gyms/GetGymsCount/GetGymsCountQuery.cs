using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Gyms.GetGymsCount;

public sealed record GetGymsCountQuery : IRequest<int>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
