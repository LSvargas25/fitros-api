using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.Logout;

public sealed class LogoutHandler : MediatR.IRequestHandler<LogoutCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ITokenService _tokens;

    public LogoutHandler(IFitRosDbContext context, ITokenService tokens)
    {
        _context = context;
        _tokens = tokens;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var hash = _tokens.HashToken(request.RefreshToken);

        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

        if (stored is null)
            return; // Idempotent

        stored.Revoke(DateTime.UtcNow);

        await _context.SaveChangesAsync(ct);
    }
}