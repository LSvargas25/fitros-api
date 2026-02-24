using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler
    : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordResetTokenGenerator _tokenGenerator;

    public ForgotPasswordHandler(
        IFitRosDbContext context,
        IPasswordResetTokenGenerator tokenGenerator)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
    }

    public async Task Handle(
        ForgotPasswordCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, ct);

        if (user is null)
            return; // Do NOT reveal existence

        var rawToken = _tokenGenerator.Generate();
        var tokenHash = _tokenGenerator.Hash(rawToken);

        var expires = DateTime.UtcNow.AddMinutes(30);

        user.SetPasswordResetToken(tokenHash, expires);

        await _context.SaveChangesAsync(ct);

        // Aquí NO enviamos email todavía.
        // Eso lo hará Infrastructure (email service).
    }
}