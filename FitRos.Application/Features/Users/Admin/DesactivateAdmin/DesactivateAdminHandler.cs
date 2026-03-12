using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Admin.DesactivateAdmin;

public sealed class DesactivateAdminHandler : IRequestHandler<DesactivateAdminCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DesactivateAdminHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DesactivateAdminCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var admin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.AdminId && x.Role == UserRole.Admin, ct);

        if (admin is null)
            throw new NotFoundException("Admin not found.");

        if (_currentUser.UserId == admin.Id)
            throw new DomainException("You cannot deactivate yourself.");

        admin.Deactivate();

        await _context.SaveChangesAsync(ct);
    }
}
