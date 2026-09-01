using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUser = FitRos.Domain.Entities.Users.User;

namespace FitRos.Application.Features.Users.Admin.DeleteAdmin;

public sealed class DeleteAdminHandler : IRequestHandler<DeleteAdminCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteAdminHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAdminCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var admin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.AdminId && x.Role == UserRole.Admin, ct);

        if (admin is null)
            throw new NotFoundException("Admin not found.");

        if (_currentUser.UserId == admin.Id)
            throw new DomainException("You cannot delete yourself.");

        if (admin.Status != UserStatus.Inactive)
            throw new DomainException("Admin must be deactivated before permanent deletion.");

        var adminCount = await _context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.Role == UserRole.Admin, ct);

        if (adminCount == 1)
            throw new DomainException("Cannot delete the last Admin.");

        _context.Remove(admin);

        await _context.SaveChangesAsync(ct);
    }
}
