using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordHandler
    : IRequestHandler<ResetPasswordCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordResetTokenGenerator _tokenGenerator;
    private readonly Func<DateTime> _utcNow;

    public ResetPasswordHandler(
        IFitRosDbContext context,
        IPasswordHasher hasher,
        IPasswordResetTokenGenerator tokenGenerator)
    {
        _context = context;
        _hasher = hasher;
        _tokenGenerator = tokenGenerator;
        _utcNow = () => DateTime.UtcNow;
    }

    public async Task Handle(
       ResetPasswordCommand request,
       CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
     .IgnoreQueryFilters()
     .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

        if (user is null)
            throw new DomainException("Invalid reset token.");

        if (user.Status != UserStatus.Active)
            throw new DomainException("Invalid reset token.");

        var cleanToken = request.Token.Trim();
        var providedHash = _tokenGenerator.Hash(cleanToken);

        if (!user.HasValidPasswordResetToken(providedHash, _utcNow()))
            throw new DomainException("Invalid reset token."); ;

        var newHash = _hasher.Hash(request.NewPassword);

        user.ChangePasswordHash(newHash);
        user.ClearPasswordResetToken();

          var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == user.Id)
            .ToListAsync(ct);

        _context.RefreshTokens.RemoveRange(tokens);

        await _context.SaveChangesAsync(ct);
    }
}