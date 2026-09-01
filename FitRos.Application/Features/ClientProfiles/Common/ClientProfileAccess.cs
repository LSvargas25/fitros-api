using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;

namespace FitRos.Application.Features.ClientProfiles.Common;

/// <summary>
/// Same tenant + role check ProgressReportAccess already applies to client
/// analytics, collected here for the plain client-profile handlers (get by
/// id, activate/deactivate). Owner: any client. Admin: any client in their
/// gym. Coach: only their assigned client. Client: only their own profile
/// (view only, not manage).
/// </summary>
internal static class ClientProfileAccess
{
    public static void EnsureCanView(ICurrentUser currentUser, ClientProfile profile)
    {
        if (currentUser.IsOwner())
            return;

        if (profile.GymId != currentUser.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        if (currentUser.IsAdmin())
            return;

        if (currentUser.IsCoach())
        {
            if (profile.CoachId != currentUser.UserId)
                throw new ForbiddenException("You do not own this client.");
            return;
        }

        if (currentUser.Role == UserRole.Client)
        {
            if (profile.UserId != currentUser.UserId)
                throw new ForbiddenException("You are not authorized.");
            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }

    public static void EnsureCanManage(ICurrentUser currentUser, ClientProfile profile)
    {
        if (currentUser.IsOwner())
            return;

        if (profile.GymId != currentUser.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        if (currentUser.IsAdmin())
            return;

        if (currentUser.IsCoach())
        {
            if (profile.CoachId != currentUser.UserId)
                throw new ForbiddenException("You do not own this client.");
            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }
}
