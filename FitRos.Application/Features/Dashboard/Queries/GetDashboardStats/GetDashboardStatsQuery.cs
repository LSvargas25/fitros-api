using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Dashboard.Queries.GetDashboardStats
{
    // OwnerApp  -> platform-wide stats (all gyms, all head counts).
    // Admin/Coach -> the same shape, but every number is scoped to the caller's
    //                own gym (same tenant-guard rule as ClientProfiles /
    //                UserManagement / Gyms). A gym Admin/Coach must never see
    //                another tenant's names or numbers.
    // Client    -> 403 (not in AllowedRoles).
    public record GetDashboardStatsQuery : IRequest<DashboardStatsResponse>, IAuthorizeRequest
    {
        public UserRole[] AllowedRoles =>
            new[] { UserRole.OwnerApp, UserRole.Admin, UserRole.Coach };
    }
}
