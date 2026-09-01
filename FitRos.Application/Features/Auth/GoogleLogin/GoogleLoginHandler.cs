using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.GoogleLogin;

public sealed class GoogleLoginHandler
    : IRequestHandler<GoogleLoginCommand, GoogleLoginResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordResetTokenGenerator _randomGenerator;
    private readonly ITokenService _tokens;

    public GoogleLoginHandler(
        IFitRosDbContext context,
        IGoogleTokenValidator googleValidator,
        IPasswordHasher hasher,
        IPasswordResetTokenGenerator randomGenerator,
        ITokenService tokens)
    {
        _context = context;
        _googleValidator = googleValidator;
        _hasher = hasher;
        _randomGenerator = randomGenerator;
        _tokens = tokens;
    }

    public async Task<GoogleLoginResponse> Handle(GoogleLoginCommand request, CancellationToken ct)
    {
        var googleUser = await _googleValidator.ValidateAsync(request.IdToken, ct);

        if (googleUser is null)
            throw new DomainException("Invalid Google token.");

        var normalizedEmail = googleUser.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (user is null)
        {
            // No password will ever be checked against this account through
            // /api/auth/login unless the owner later sets one via
            // forgot-password, so a random unusable value satisfies the
            // required PasswordHash column without being a real credential.
            var unusablePasswordHash = _hasher.Hash(_randomGenerator.Generate());

            user = User.Create(
                googleUser.Email,
                googleUser.FirstName,
                googleUser.LastName,
                unusablePasswordHash,
                UserRole.Client);

            if (!googleUser.EmailVerified)
                user = User.CreateUnverified(
                    googleUser.Email, googleUser.FirstName, googleUser.LastName, unusablePasswordHash, UserRole.Client);

            _context.Users.Add(user);

            var profile = ClientProfile.CreateIndependent(user.Id);
            _context.ClientProfiles.Add(profile);

            await _context.SaveChangesAsync(ct);
        }
        else if (user.Status != UserStatus.Active)
        {
            throw new DomainException("User is inactive.");
        }
        else if (!user.EmailVerified && googleUser.EmailVerified)
        {
            user.MarkEmailVerified();
            await _context.SaveChangesAsync(ct);
        }

        var access = _tokens.CreateAccessToken(user);
        var refreshPlain = _tokens.CreateRefreshTokenPlain();
        var refreshHash = _tokens.HashToken(refreshPlain);

        var refresh = RefreshToken.Create(
            userId: user.Id,
            tokenHash: refreshHash,
            expiresAt: DateTime.UtcNow.AddDays(30));

        _context.RefreshTokens.Add(refresh);
        await _context.SaveChangesAsync(ct);

        return new GoogleLoginResponse(
            user.Id,
            user.Email,
            (int)user.Role,
            access,
            refreshPlain);
    }
}
