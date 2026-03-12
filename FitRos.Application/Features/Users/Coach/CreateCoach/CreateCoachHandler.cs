using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Coach.CreateCoach;

public sealed class CreateCoachHandler
    : IRequestHandler<CreateCoachCommand, CreateUserResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUser _currentUser;

    public CreateCoachHandler(
        IFitRosDbContext context,
        IPasswordHasher hasher,
        ICurrentUser currentUser)
    {
        _context = context;
        _hasher = hasher;
        _currentUser = currentUser;
    }

    public async Task<CreateUserResponse> Handle(
        CreateCoachCommand request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        Guid? gymId = _currentUser.IsOwner()
            ? request.GymId
            : _currentUser.GymId ?? throw new DomainException("Current user is not assigned to a gym.");

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var exists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (exists)
            throw new DomainException("Email already exists.");

        var passwordHash = _hasher.Hash(request.Password);

        var user = gymId.HasValue
            ? User.CreateForGym(gymId.Value, request.Email, request.FirstName, request.LastName, passwordHash, UserRole.Coach)
            : User.Create(request.Email, request.FirstName, request.LastName, passwordHash, UserRole.Coach);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        var coachFullName = $"{user.FirstName} {user.LastName}";

        var recipients = await _context.Users
            .Where(u => u.Role == UserRole.OwnerApp ||
                        gymId.HasValue && u.Role == UserRole.Admin && u.GymId == gymId)
            .ToListAsync(ct);

        foreach (var recipient in recipients)
        {
            var notification = NotificationFactory.CoachCreated(recipient.Id, gymId, coachFullName);
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync(ct);

        return new CreateUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            (int)user.Role);
    }
}
