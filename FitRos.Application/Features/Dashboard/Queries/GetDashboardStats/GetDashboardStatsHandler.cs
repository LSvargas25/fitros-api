using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
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
        var isOwner = _currentUser.IsOwner();

        // Admin / Coach with no gym assigned: an empty dashboard, not an error.
        if (!isOwner && _currentUser.GymId is null)
            return new DashboardStatsResponse(0, 0, 0, 0, 0, Array.Empty<GymSummaryDto>());

        var scopeGymId = _currentUser.GymId;

        // Bypass the ambient tenant query filter and scope explicitly. For the
        // OwnerApp (GymId == null) the filter would otherwise collapse every
        // tenant-scoped count to "GymId IS NULL" (~0). Only active users are
        // counted (a removed coach is deactivated, not deleted) and soft-deleted
        // clients are excluded, matching the entities' own query filters.
        var users = _context.Users.IgnoreQueryFilters()
            .Where(u => u.Status == UserStatus.Active);
        var clients = _context.ClientProfiles.IgnoreQueryFilters()
            .Where(c => c.Status != ClientStatus.Deleted);
        var routines = _context.WorkoutRoutines.IgnoreQueryFilters();
        var gyms = _context.Gyms.Where(g => !g.IsDeleted);

        if (!isOwner)
        {
            users = users.Where(u => u.GymId == scopeGymId);
            clients = clients.Where(c => c.GymId == scopeGymId);
            routines = routines.Where(r => r.GymId == scopeGymId);
            gyms = gyms.Where(g => g.Id == scopeGymId);
        }

        var totalGyms = await gyms.CountAsync(ct);
        var totalAdmins = await users.CountAsync(u => u.Role == UserRole.Admin, ct);
        var totalCoaches = await users.CountAsync(u => u.Role == UserRole.Coach, ct);
        var totalClients = await clients.CountAsync(ct);
        var totalRoutines = await routines.CountAsync(ct);

        var clientCounts = await clients
            .Where(c => c.GymId != null)
            .GroupBy(c => c.GymId!.Value)
            .Select(g => new { GymId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GymId, x => x.Count, ct);

        var coachCounts = await users
            .Where(u => u.Role == UserRole.Coach && u.GymId != null)
            .GroupBy(u => u.GymId!.Value)
            .Select(g => new { GymId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GymId, x => x.Count, ct);

        var adminCounts = await users
            .Where(u => u.Role == UserRole.Admin && u.GymId != null)
            .GroupBy(u => u.GymId!.Value)
            .Select(g => new { GymId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GymId, x => x.Count, ct);

        var gymRows = await gyms
            .Select(g => new { g.Id, g.Name, g.PhoneNumber, g.IsActive })
            .ToListAsync(ct);

        var gymDtos = gymRows
            .Select(g => new GymSummaryDto(
                g.Id,
                g.Name,
                g.PhoneNumber,
                g.IsActive,
                clientCounts.GetValueOrDefault(g.Id),
                coachCounts.GetValueOrDefault(g.Id),
                adminCounts.GetValueOrDefault(g.Id)))
            .ToList();

        return new DashboardStatsResponse(
            totalGyms, totalAdmins, totalClients, totalCoaches, totalRoutines, gymDtos);
    }
}
