using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Admin.CreateAdmin;

public sealed class CreateAdminHandler : IRequestHandler<CreateAdminCommand, CreateUserResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _hasher;

    public CreateAdminHandler(IFitRosDbContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task<CreateUserResponse> Handle(CreateAdminCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var exists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (exists)
            throw new DomainException("Email already exists.");

        var passwordHash = _hasher.Hash(request.Password);

        var admin = User.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash,
            UserRole.Admin);

        _context.Users.Add(admin);

        await _context.SaveChangesAsync(ct);

        var adminFullName = $"{admin.FirstName} {admin.LastName}";

        var ownerAppUsers = await _context.Users
            .Where(u => u.Role == UserRole.OwnerApp)
            .ToListAsync(ct);

        foreach (var owner in ownerAppUsers)
        {
            var notification = NotificationFactory.AdminCreated(owner.Id, adminFullName);
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync(ct);

        return new CreateUserResponse(
            admin.Id,
            admin.Email,
            admin.FirstName,
            admin.LastName,
            (int)admin.Role);
    }
}
