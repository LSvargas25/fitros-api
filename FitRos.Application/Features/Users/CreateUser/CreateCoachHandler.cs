using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.CreateUser;

public sealed class CreateCoachHandler
    : IRequestHandler<CreateCoachCommand, CreateUserResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _hasher;

    public CreateCoachHandler(
        IFitRosDbContext context,
        IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task<CreateUserResponse> Handle(
        CreateCoachCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        // Ensure email uniqueness
        var emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (emailExists)
            throw new DomainException("Email already exists.");

        var passwordHash = _hasher.Hash(request.Password);

        var user = User.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash,
            UserRole.Coach);

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