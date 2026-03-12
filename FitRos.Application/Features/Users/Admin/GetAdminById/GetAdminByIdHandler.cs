using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Admin.GetAdminById;

public sealed class GetAdminByIdHandler : IRequestHandler<GetAdminByIdQuery, AdminDetailDto?>
{
    private readonly IFitRosDbContext _context;

    public GetAdminByIdHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDetailDto?> Handle(
        GetAdminByIdQuery request,
        CancellationToken cancellationToken)
    {
        var admin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Id == request.AdminId && u.Role == UserRole.Admin,
                cancellationToken);

        if (admin is null)
            return null;

        string? gymName = null;
        if (admin.GymId.HasValue)
        {
            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == admin.GymId.Value, cancellationToken);
            gymName = gym?.Name;
        }

        return new AdminDetailDto(
            admin.Id,
            admin.Email,
            admin.FirstName,
            admin.LastName,
            admin.Status.ToString(),
            admin.GymId,
            gymName);
    }
}
