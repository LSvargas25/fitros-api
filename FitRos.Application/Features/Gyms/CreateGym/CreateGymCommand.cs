using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Gyms.CreateGym;

public record CreateGymCommand(
    string Name,
    string Address,
    string PhoneNumber
) : IRequest<Guid>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
