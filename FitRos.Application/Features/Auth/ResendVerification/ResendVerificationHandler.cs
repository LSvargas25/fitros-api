using FitRos.Application.Abstractions.Messaging;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.ResendVerification;

public sealed class ResendVerificationHandler : IRequestHandler<ResendVerificationCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly IEmailVerificationCodeGenerator _codeGenerator;
    private readonly IEmailSender _emailSender;

    public ResendVerificationHandler(
        IFitRosDbContext context,
        IEmailVerificationCodeGenerator codeGenerator,
        IEmailSender emailSender)
    {
        _context = context;
        _codeGenerator = codeGenerator;
        _emailSender = emailSender;
    }

    public async Task Handle(ResendVerificationCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        // Don't reveal whether the account exists or is already verified.
        if (user is null || user.EmailVerified)
            return;

        var code = _codeGenerator.GenerateCode();
        var codeHash = _codeGenerator.Hash(code);
        user.SetEmailVerificationCode(codeHash, DateTime.UtcNow.AddMinutes(15));

        await _context.SaveChangesAsync(ct);

        var subject = "Your FitRos verification code";
        var html = $@"
<div style='font-family:Arial,sans-serif; line-height:1.5; color:#0f172a;'>
  <h2 style='margin:0 0 12px 0;'>Verify your email</h2>
  <p style='margin:0 0 12px 0;'>
    Here's your new verification code:
  </p>
  <p style='margin:0 0 20px 0; font-size:28px; font-weight:bold; letter-spacing:4px;'>
    {code}
  </p>
  <p style='margin:0; font-size:12px; color:#475569;'>
    This code expires in <strong>15 minutes</strong>. If you didn't request this, you can ignore this email.
  </p>
</div>";

        await _emailSender.SendAsync(user.Email, subject, html, ct);
    }
}
