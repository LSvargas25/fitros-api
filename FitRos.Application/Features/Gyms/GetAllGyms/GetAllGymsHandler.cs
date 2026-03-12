using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.GetAllGyms;

public sealed class GetAllGymsHandler : IRequestHandler<GetAllGymsQuery, List<GymListItemDto>>
{
    private readonly IFitRosDbContext _context;

    public GetAllGymsHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<List<GymListItemDto>> Handle(
        GetAllGymsQuery request,
        CancellationToken cancellationToken)
    {
        var gyms = await _context.Gyms
            .Where(g => !g.IsDeleted)
            .ToListAsync(cancellationToken);

        var gymIds = gyms.Select(g => g.Id).ToList();

        var admins = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Admin && u.GymId != null && gymIds.Contains(u.GymId.Value))
            .ToListAsync(cancellationToken);

        var adminsByGym = admins
      .GroupBy(u => u.GymId!.Value)
      .ToDictionary(
          g => g.Key,
          g => g.Select(u => new GymAdminDto(u.Id, $"{u.FirstName} {u.LastName}")).ToList()
      );

        return gyms.Select(g =>
        {
            adminsByGym.TryGetValue(g.Id, out var gymAdmins);
            return new GymListItemDto(
                g.Id,
                g.Name,
                g.Address,
                g.PhoneNumber,
                g.IsActive,
                gymAdmins ?? new List<GymAdminDto>());
        }).ToList();
    }
}
