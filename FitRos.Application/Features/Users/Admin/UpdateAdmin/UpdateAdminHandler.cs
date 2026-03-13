using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Users.Admin.GetAdminById;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Admin.UpdateAdmin;

public sealed class UpdateAdminHandler : IRequestHandler<UpdateAdminCommand, AdminDetailDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateAdminHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<AdminDetailDto> Handle(UpdateAdminCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var admin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.AdminId && u.Role == UserRole.Admin, ct);

        if (admin is null)
            throw new NotFoundException("Admin not found.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalized = request.Email.Trim().ToUpperInvariant();
            var exists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalized && u.Id != admin.Id, ct);

            if (exists)
                throw new DomainException("Email already in use.");

            admin.ChangeEmail(request.Email);
        }

        admin.UpdateBasicInfo(
            string.IsNullOrWhiteSpace(request.FirstName) ? admin.FirstName : request.FirstName,
            string.IsNullOrWhiteSpace(request.LastName) ? admin.LastName : request.LastName);

        await _context.SaveChangesAsync(ct);

        string? gymName = null;
        if (admin.GymId.HasValue)
        {
            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == admin.GymId.Value, ct);
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
