using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Dashboard.Queries.GetDashboardStats
{
    // Platform-wide stats (all gyms, all admins, per-gym head counts) — OwnerApp
    // only. A gym Admin/Coach must never see other tenants' names or numbers.
    public record GetDashboardStatsQuery : IRequest<DashboardStatsResponse>, IAuthorizeRequest
    {
        public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
    }
}
