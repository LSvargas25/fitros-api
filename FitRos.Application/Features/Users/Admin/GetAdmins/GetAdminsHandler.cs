using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Admin.GetAdmins;

public sealed class GetAdminsHandler : IRequestHandler<GetAdminsQuery, List<AdminListItemDto>>
{
    private readonly IFitRosDbContext _context;

    public GetAdminsHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdminListItemDto>> Handle(
        GetAdminsQuery request,
        CancellationToken cancellationToken)
    {
        var admins = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Admin)
            .ToListAsync(cancellationToken);

        var gymIds = admins
            .Where(a => a.GymId.HasValue)
            .Select(a => a.GymId!.Value)
            .Distinct()
            .ToList();

        var gyms = await _context.Gyms
            .Where(g => gymIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);

        return admins.Select(a =>
        {
            gyms.TryGetValue(a.GymId ?? Guid.Empty, out var gymName);
            return new AdminListItemDto(
                a.Id,
                a.Email,
                a.FirstName,
                a.LastName,
                a.Status.ToString(),
                a.GymId,
                a.GymId.HasValue ? gymName : null);
        }).ToList();
    }
}
