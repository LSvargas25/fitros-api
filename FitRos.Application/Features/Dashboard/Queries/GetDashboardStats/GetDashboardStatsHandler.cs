using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Dashboard.Queries.GetDashboardStats;

public sealed class GetDashboardStatsHandler
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    private readonly IFitRosDbContext _context;
    public GetDashboardStatsHandler(IFitRosDbContext context) => _context = context;

    public async Task<DashboardStatsResponse> Handle(
        GetDashboardStatsQuery request, CancellationToken ct)
    {
        var totalGyms = await _context.Gyms.CountAsync(ct);
        var totalClients = await _context.ClientProfiles.CountAsync(ct);
        var totalCoaches = await _context.Users
            .CountAsync(u => u.Role == UserRole.Coach, ct);
        var totalRoutines = await _context.WorkoutRoutines.CountAsync(ct);

        
        var gyms = await _context.Gyms
            .Where(g => !g.IsDeleted)
            .Select(g => new GymSummaryDto(
                g.Id,
                g.Name,
                g.PhoneNumber,
                g.IsActive,
                _context.ClientProfiles.Count(c => c.GymId == g.Id),
                _context.Users.Count(u => u.GymId == g.Id && u.Role == UserRole.Coach),
                _context.Users.Count(u => u.GymId == g.Id && u.Role == UserRole.Admin)
            ))
            .ToListAsync(ct);

        return new DashboardStatsResponse(
            totalGyms, totalClients, totalCoaches, totalRoutines, gyms);
    }
}