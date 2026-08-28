 
using FitRos.Application.Features.Dashboard.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "OwnerApp")]
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