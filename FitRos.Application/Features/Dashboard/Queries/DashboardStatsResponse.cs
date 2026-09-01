using FitRos.Application.Features.Dashboard.Queries.GetDashboardStats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Dashboard.Queries
{
    public record DashboardStatsResponse(
      int TotalGyms,
      int TotalAdmins,
      int TotalClients,
      int TotalCoaches,
      int TotalRoutines,
      IReadOnlyList<GymSummaryDto> Gyms
  );
}
