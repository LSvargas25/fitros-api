using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;

namespace FitRos.Application.Common.Security;

/// <summary>
/// Shared rule for "may this coach act on this <see cref="ClientProfile"/> (and
/// the meal / training plans hanging off it)?".
///
/// A coach is responsible for a profile when they are its assigned coach, OR
/// when it is their own personal <c>CoachSelf</c> profile — those carry a null
/// <c>CoachId</c> (see BACKEND_AUDIT §19) and are identified by <c>UserId</c>.
/// Checking only <c>CoachId == UserId</c> wrongly 403s a coach on their own
/// area (BACKEND_AUDIT §25).
/// </summary>
public static class CoachClientAccess
{
    public static bool CoachIsResponsibleFor(this ICurrentUser user, ClientProfile profile)
        => profile.CoachId == user.UserId || profile.UserId == user.UserId;

    /// <summary>
    /// Throws <see cref="ForbiddenException"/> if the caller is a coach who is
    /// neither the profile's assigned coach nor its owner. No-op for every other
    /// role (gym / ownership checks live in the caller).
    /// </summary>
    public static void EnsureCoachCanAccess(
        this ICurrentUser user,
        ClientProfile profile,
        string message = "You are not the assigned coach for this client.")
    {
        if (user.IsCoach() && !user.CoachIsResponsibleFor(profile))
            throw new ForbiddenException(message);
    }
}
