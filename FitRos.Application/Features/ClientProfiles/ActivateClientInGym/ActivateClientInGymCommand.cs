using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.ClientProfiles.ActivateClientInGym;

public sealed record ActivateClientInGymCommand(Guid ClientProfileId, Guid GymId) : IRequest, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin };
}
