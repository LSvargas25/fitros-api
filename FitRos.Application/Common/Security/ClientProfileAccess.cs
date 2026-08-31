using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;

namespace FitRos.Application.Common.Security;

/// <summary>
/// The one role + tenant rule for reading/writing a client's own data
/// (measures, KPI snapshots, progress reports):
///
/// <list type="bullet">
///   <item>OwnerApp — any client, any gym.</item>
///   <item>Admin — any client in their own gym.</item>
///   <item>Coach — a client assigned to them, or their own CoachSelf profile
///         (whose <c>CoachId</c> is null — see BACKEND_AUDIT §19 / §25).</item>
///   <item>Client — only their own profile.</item>
/// </list>
///
/// Replaces the per-handler <c>ValidatePermissions</c> copies that (a) 403'd a
/// coach on their own area and (b) 403'd OwnerApp on the two measures handlers
/// by running a gym check before the owner short-circuit — see BACKEND_AUDIT §26.
/// </summary>
public static class ClientProfileAccess
{
    public static void EnsureCanAccess(this ICurrentUser user, ClientProfile profile)
    {
        if (!user.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        if (user.IsOwner())
            return;

        if (profile.GymId != user.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        if (user.IsAdmin())
            return;

        if (user.IsCoach())
        {
            if (!user.CoachIsResponsibleFor(profile))
                throw new ForbiddenException("You do not own this client.");
            return;
        }

        if (user.IsClient())
        {
            if (profile.UserId != user.UserId)
                throw new ForbiddenException("You are not authorized.");
            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }

    /// <summary>
    /// Managing a client profile (activate/deactivate): OwnerApp any client,
    /// Admin any client in their gym, Coach only a client assigned to them.
    /// A Client cannot manage profiles, not even their own.
    /// </summary>
    public static void EnsureCanManage(this ICurrentUser user, ClientProfile profile)
    {
        if (!user.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        if (user.IsOwner())
            return;

        if (profile.GymId != user.GymId)
            throw new ForbiddenException("Client does not belong to your gym.");

        if (user.IsAdmin())
            return;

        if (user.IsCoach())
        {
            if (profile.CoachId != user.UserId)
                throw new ForbiddenException("You do not own this client.");
            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }
}
