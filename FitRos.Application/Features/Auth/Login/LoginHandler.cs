using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.Login;

public sealed class LoginHandler
    : MediatR.IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly Func<DateTime> _utcNow;

    public LoginHandler(
        IFitRosDbContext context,
        IPasswordHasher hasher,
        ITokenService tokens)
    {
        _context = context;
        _hasher = hasher;
        _tokens = tokens;
        _utcNow = () => DateTime.UtcNow;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

        if (user is null)
            throw new DomainException("Invalid credentials.");

        if (user.Status != UserStatus.Active)
            throw new DomainException("User is inactive.");
 
        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Invalid credentials.");

        var access = _tokens.CreateAccessToken(user);

        var refreshPlain = _tokens.CreateRefreshTokenPlain();
        var refreshHash = _tokens.HashToken(refreshPlain);

        var refresh = RefreshToken.Create(
            userId: user.Id,
            tokenHash: refreshHash,
            expiresAt: _utcNow().AddDays(30));

        _context.RefreshTokens.Add(refresh);
        await _context.SaveChangesAsync(ct);

        return new LoginResponse(
            user.Id,
            user.Email,
            (int)user.Role,
            access,
            refreshPlain);
    }
}