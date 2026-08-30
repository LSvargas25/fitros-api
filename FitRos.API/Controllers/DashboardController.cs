 
using FitRos.Application.Features.Dashboard.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// OwnerApp sees platform-wide stats; Admin/Coach see the same shape scoped to
// their own gym (enforced in GetDashboardStatsHandler). Client stays 403.
[Authorize(Roles = "OwnerApp,Admin,Coach")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;
    public DashboardController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _sender.Send(new GetDashboardStatsQuery(), ct);
        return Ok(result);
    }
}