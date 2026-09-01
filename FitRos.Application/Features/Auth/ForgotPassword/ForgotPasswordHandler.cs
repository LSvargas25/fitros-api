using FitRos.Application.Abstractions.Messaging;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitRos.Application.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler
    : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordResetTokenGenerator _tokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly FrontendSettings _frontend;

    public ForgotPasswordHandler(
        IFitRosDbContext context,
        IPasswordResetTokenGenerator tokenGenerator,
        IEmailSender emailSender,
        IOptions<FrontendSettings> frontendOptions)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _emailSender = emailSender;
        _frontend = frontendOptions.Value;
    }

    public async Task Handle(
        ForgotPasswordCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, ct);

        if (user is null)
            return; // Do NOT reveal existence

        var rawToken = _tokenGenerator.Generate();
        var tokenHash = _tokenGenerator.Hash(rawToken);

        var expires = DateTime.UtcNow.AddMinutes(30);

        user.SetPasswordResetToken(tokenHash, expires);
        await _context.SaveChangesAsync(ct);

        // Build reset link (URL encoded)
        var encodedToken = Uri.EscapeDataString(rawToken);
        var encodedEmail = Uri.EscapeDataString(user.Email);

        var baseUrl = _frontend.BaseUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

        var subject = "Reset your FitRos password";
        var html = $@"
<div style='font-family:Arial,sans-serif; line-height:1.5; color:#0f172a;'>
  <h2 style='margin:0 0 12px 0;'>Reset your password</h2>
  <p style='margin:0 0 12px 0;'>
    We received a request to reset your password. If you didn't request this, you can ignore this email.
  </p>
  <p style='margin:0 0 16px 0;'>
    This link expires in <strong>30 minutes</strong>.
  </p>
  <p style='margin:0 0 20px 0;'>
    <a href='{resetLink}' style='display:inline-block; padding:10px 16px; text-decoration:none; border-radius:8px; background:#0f172a; color:#ffffff;'>
      Reset Password
    </a>
  </p>
  <p style='margin:0; font-size:12px; color:#475569;'>
    If the button doesn't work, copy and paste this link into your browser:<br/>
    <span style='word-break:break-all;'>{resetLink}</span>
  </p>
</div>";

        await _emailSender.SendAsync(user.Email, subject, html, ct);
    }
}