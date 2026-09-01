using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Auth.VerifyEmail;

public sealed class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly IEmailVerificationCodeGenerator _codeGenerator;

    public VerifyEmailHandler(
        IFitRosDbContext context,
        IEmailVerificationCodeGenerator codeGenerator)
    {
        _context = context;
        _codeGenerator = codeGenerator;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        var codeHash = _codeGenerator.Hash(request.Code.Trim());

        if (user is null || !user.HasValidEmailVerificationCode(codeHash, DateTime.UtcNow))
            throw new DomainException("Invalid or expired verification code.");

        user.MarkEmailVerified();

        await _context.SaveChangesAsync(ct);
    }
}
