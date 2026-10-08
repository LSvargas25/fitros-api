using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Dashboard.Queries.GetDashboardStats;

public sealed class GetDashboardStatsHandler
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetDashboardStatsHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DashboardStatsResponse> Handle(
        GetDashboardStatsQuery request, CancellationToken ct)
    {
        // The tenant query filter scopes rows to the caller's GymId. The
        // platform owner has none, so through the filter every per-gym count
        // came back 0; the owner gets platform-wide counts instead, with the
        // non-tenant parts of the filters (active users, non-deleted clients)
        // applied by hand.
        var platformWide = _currentUser.Role == UserRole.OwnerApp;

        var users = platformWide
            ? _context.Users.IgnoreQueryFilters().Where(u => u.Status == UserStatus.Active)
            : _context.Users;

        var clients = platformWide
            ? _context.ClientProfiles.IgnoreQueryFilters().Where(c => c.Status != ClientStatus.Deleted)
            : _context.ClientProfiles;

        var routines = platformWide
            ? _context.WorkoutRoutines.IgnoreQueryFilters()
            : _context.WorkoutRoutines;

        var totalGyms = await _context.Gyms.CountAsync(ct);

        var totalAdmins = await (platformWide ? users : _context.Users.IgnoreQueryFilters())
            .CountAsync(u => u.Role == UserRole.Admin, ct);

        var totalClients = await clients.CountAsync(ct);

        var totalCoaches = await users
            .CountAsync(u => u.Role == UserRole.Coach, ct);

        var totalRoutines = await routines.CountAsync(ct);

        var gyms = await _context.Gyms
            .Where(g => !g.IsDeleted)
            .Select(g => new GymSummaryDto(
                g.Id,
                g.Name,
                g.PhoneNumber,
                g.IsActive,
                clients.Count(c => c.GymId == g.Id),
                users.Count(u => u.GymId == g.Id && u.Role == UserRole.Coach),
                users.Count(u => u.GymId == g.Id && u.Role == UserRole.Admin)
            ))
            .ToListAsync(ct);

        return new DashboardStatsResponse(
            totalGyms, totalAdmins, totalClients, totalCoaches, totalRoutines, gyms);
    }
}