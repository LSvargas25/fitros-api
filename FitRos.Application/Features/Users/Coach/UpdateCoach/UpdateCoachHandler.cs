using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.Coach.GetCoachById;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Coach.UpdateCoach;

public sealed class UpdateCoachHandler : IRequestHandler<UpdateCoachCommand, CoachDetailDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateCoachHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CoachDetailDto> Handle(UpdateCoachCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == request.CoachId && u.Role == UserRole.Coach);

        if (_currentUser.IsAdmin())
            query = query.Where(u => u.GymId == _currentUser.GymId);

        var coach = await query.FirstOrDefaultAsync(ct);

        if (coach is null)
            throw new NotFoundException("Coach not found.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalized = request.Email.Trim().ToUpperInvariant();
            var exists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalized && u.Id != coach.Id, ct);

            if (exists)
                throw new DomainException("Email already in use.");

            coach.ChangeEmail(request.Email);
        }

        coach.UpdateBasicInfo(
            string.IsNullOrWhiteSpace(request.FirstName) ? coach.FirstName : request.FirstName,
            string.IsNullOrWhiteSpace(request.LastName) ? coach.LastName : request.LastName);

        await _context.SaveChangesAsync(ct);

        string? gymName = null;
        if (coach.GymId.HasValue)
        {
            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == coach.GymId.Value, ct);
            gymName = gym?.Name;
        }

        return new CoachDetailDto(
            coach.Id,
            coach.Email,
            coach.FirstName,
            coach.LastName,
            coach.Status.ToString(),
            coach.GymId,
            gymName);
    }
}
