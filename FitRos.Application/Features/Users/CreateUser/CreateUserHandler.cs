using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.CreateUser;

public sealed class CreateUserHandler
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;

    public CreateUserHandler(
        IFitRosDbContext context,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<CreateUserResponse> Handle(
        CreateUserCommand command,
        UserRole roleToAssign,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new DomainException("You are not authorized.");

       
        if (_currentUser.Role == UserRole.Coach && roleToAssign != UserRole.Client)
            throw new DomainException("Coach can only create Client users.");

        if (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.Coach)
            throw new DomainException("You are not authorized.");

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();

        var exists = await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (exists)
            throw new DomainException("Email already exists.");

        var hash = _passwordHasher.Hash(command.Password);

        var user = User.Create(
            command.Email,
            command.FirstName,
            command.LastName,
            hash,
            roleToAssign);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            (int)user.Role
        );
    }
}