using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.Common;

/// <summary>
/// Same tenant + role check the other ClientProfile analytics handlers do
/// inline (KPI snapshot generate / history), collected in one place.
/// Owner: any client. Admin: any client in their gym. Coach: only their
/// assigned client. Client: only their own profile.
/// </summary>
internal static class ProgressReportAccess
{
    public static void EnsureCanView(ICurrentUser currentUser, ClientProfile profile)
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

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
}
