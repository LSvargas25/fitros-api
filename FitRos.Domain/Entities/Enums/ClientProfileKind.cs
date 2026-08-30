namespace FitRos.Domain.Entities.Enums;

/// <summary>
/// What a <see cref="FitRos.Domain.Entities.Client.ClientProfile"/> represents.
/// </summary>
public enum ClientProfileKind
{
    /// <summary>
    /// A real gym client managed by staff. Appears in coach rosters, the
    /// admin client list and dashboard client counts. This is the default and
    /// covers every profile that existed before coaches got a personal one.
    /// </summary>
    GymClient = 0,

    /// <summary>
    /// A coach's own personal training record. Its <c>UserId</c> is the coach's
    /// user id and its <c>CoachId</c> is always <c>null</c> — it is never part of
    /// any client roster or client count, and is only reachable through the
    /// JWT-resolved "/api/my/*" + "/me" + "start-today" endpoints.
    /// </summary>
    CoachSelf = 1,
}
