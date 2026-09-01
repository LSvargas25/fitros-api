using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.Refresh;

public sealed class RefreshHandler : MediatR.IRequestHandler<RefreshCommand, RefreshResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ITokenService _tokens;
    private readonly Func<DateTime> _utcNow;

    public RefreshHandler(IFitRosDbContext context, ITokenService tokens)
    {
        _context = context;
        _tokens = tokens;
        _utcNow = () => DateTime.UtcNow;
    }

    public async Task<RefreshResponse> Handle(RefreshCommand request, CancellationToken ct)
    {
        var incomingHash = _tokens.HashToken(request.RefreshToken);

        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == incomingHash, ct);

        if (stored is null)
            throw new DomainException("Invalid refresh token.");

        if (stored.IsExpired(_utcNow()))
            throw new DomainException("Refresh token is not valid.");

        if (stored.IsRevoked)
        {
             
            var activeTokens = await _context.RefreshTokens
                .Where(x => x.UserId == stored.UserId && !x.IsRevoked)
                .ToListAsync(ct);

            foreach (var t in activeTokens)
                t.Revoke(_utcNow());

            await _context.SaveChangesAsync(ct);

            throw new DomainException("Refresh token is not valid.");
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);

        if (user is null || user.Status != Domain.Enums.UserStatus.Active)
            throw new DomainException("User is not valid.");

        var newAccess = _tokens.CreateAccessToken(user);

        var newRefreshPlain = _tokens.CreateRefreshTokenPlain();
        var newRefreshHash = _tokens.HashToken(newRefreshPlain);

        stored.Revoke(_utcNow(), replacedByTokenHash: newRefreshHash);

        var newRefresh = RefreshToken.Create(
            user.Id,
            newRefreshHash,
            _utcNow().AddDays(30));

        _context.RefreshTokens.Add(newRefresh);

        await _context.SaveChangesAsync(ct);

        return new RefreshResponse(newAccess, newRefreshPlain);
    }
}