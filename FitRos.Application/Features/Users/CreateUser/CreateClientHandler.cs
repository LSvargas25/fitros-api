 using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Users.CreateUser;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.CreateUser;

public sealed class CreateClientHandler
: IRequestHandler<CreateClientCommand, CreateUserResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUser _currentUser;

 
public CreateClientHandler(
    IFitRosDbContext context,
    IPasswordHasher hasher,
    ICurrentUser currentUser)
    {
        _context = context;
        _hasher = hasher;
        _currentUser = currentUser;
    }

    public async Task<CreateUserResponse> Handle(
        CreateClientCommand request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        if (_currentUser.GymId is null)
            throw new DomainException("Current user is not assigned to a gym.");

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var exists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (exists)
            throw new DomainException("Email already exists.");

        var passwordHash = _hasher.Hash(request.Password);

        var user = User.CreateForGym(
            _currentUser.GymId.Value,
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash,
            UserRole.Client);

        _context.Users.Add(user);

        await _context.SaveChangesAsync(ct);

        return new CreateUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            (int)user.Role);
    }
}

 