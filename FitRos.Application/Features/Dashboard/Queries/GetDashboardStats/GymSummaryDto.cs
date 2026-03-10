using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Dashboard.Queries.GetDashboardStats
{
    public record GymSummaryDto(
     Guid Id,
     string Name,
     string PhoneNumber,
     bool IsActive,
     int ClientCount,
     int CoachCount,
     int AdminCount
 );

}
