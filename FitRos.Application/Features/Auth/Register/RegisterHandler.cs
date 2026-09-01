using FitRos.Application.Abstractions.Messaging;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.Register;

public sealed class RegisterHandler
    : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailVerificationCodeGenerator _codeGenerator;
    private readonly IEmailSender _emailSender;

    public RegisterHandler(
        IFitRosDbContext context,
        IPasswordHasher hasher,
        IEmailVerificationCodeGenerator codeGenerator,
        IEmailSender emailSender)
    {
        _context = context;
        _hasher = hasher;
        _codeGenerator = codeGenerator;
        _emailSender = emailSender;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var exists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (exists)
            throw new DomainException("Email already exists.");

        var passwordHash = _hasher.Hash(request.Password);

        var user = User.CreateUnverified(
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash,
            UserRole.Client);

        _context.Users.Add(user);

        var profile = ClientProfile.CreateIndependent(user.Id);
        _context.ClientProfiles.Add(profile);

        var code = _codeGenerator.GenerateCode();
        var codeHash = _codeGenerator.Hash(code);
        user.SetEmailVerificationCode(codeHash, DateTime.UtcNow.AddMinutes(15));

        await _context.SaveChangesAsync(ct);

        var subject = "Verify your FitRos email";
        var html = $@"
<div style='font-family:Arial,sans-serif; line-height:1.5; color:#0f172a;'>
  <h2 style='margin:0 0 12px 0;'>Verify your email</h2>
  <p style='margin:0 0 12px 0;'>
    Use this code to finish creating your FitRos account:
  </p>
  <p style='margin:0 0 20px 0; font-size:28px; font-weight:bold; letter-spacing:4px;'>
    {code}
  </p>
  <p style='margin:0; font-size:12px; color:#475569;'>
    This code expires in <strong>15 minutes</strong>. If you didn't request this, you can ignore this email.
  </p>
</div>";

        await _emailSender.SendAsync(user.Email, subject, html, ct);

        return new RegisterResponse(user.Id, user.Email);
    }
}
