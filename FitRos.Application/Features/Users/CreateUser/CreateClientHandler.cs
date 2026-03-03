using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
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

    public CreateClientHandler(
        IFitRosDbContext context,
        IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task<CreateUserResponse> Handle(
        CreateClientCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var exists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (exists)
            throw new DomainException("Email already exists.");

        var passwordHash = _hasher.Hash(request.Password);

        var user = User.Create(
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