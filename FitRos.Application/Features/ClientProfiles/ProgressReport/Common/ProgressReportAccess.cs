using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.Common;

/// <summary>
/// Thin alias over <see cref="ClientProfileAccess.EnsureCanAccess"/> — the one
/// role + tenant rule for a client's own data. Kept as a named entry point for
/// the three progress-report handlers. Owner: any. Admin: own gym. Coach:
/// assigned client OR their own CoachSelf profile. Client: own profile only.
/// </summary>
internal static class ProgressReportAccess
{
    public static void EnsureCanView(ICurrentUser currentUser, ClientProfile profile)
        => currentUser.EnsureCanAccess(profile);
}
