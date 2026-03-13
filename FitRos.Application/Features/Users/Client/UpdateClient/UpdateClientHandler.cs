using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.Client.GetClientById;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Client.UpdateClient;

public sealed class UpdateClientHandler : IRequestHandler<UpdateClientCommand, ClientUserDetailDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateClientHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ClientUserDetailDto> Handle(UpdateClientCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var client = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.ClientId && u.Role == UserRole.Client, ct);

        if (client is null)
            throw new NotFoundException("Client not found.");

        if (_currentUser.IsAdmin() && client.GymId != _currentUser.GymId)
            throw new ForbiddenException("Admins can only update clients within their gym.");

        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cp => cp.UserId == client.Id, ct);

        if (_currentUser.IsCoach())
        {
            if (profile is null || profile.CoachId != _currentUser.UserId)
                throw new ForbiddenException("Coaches can only update their own clients.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalized = request.Email.Trim().ToUpperInvariant();
            var exists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalized && u.Id != client.Id, ct);

            if (exists)
                throw new DomainException("Email already in use.");

            client.ChangeEmail(request.Email);
        }

        client.UpdateBasicInfo(
            string.IsNullOrWhiteSpace(request.FirstName) ? client.FirstName : request.FirstName,
            string.IsNullOrWhiteSpace(request.LastName) ? client.LastName : request.LastName);

        await _context.SaveChangesAsync(ct);

        string? coachName = null;
        if (profile?.CoachId is not null)
        {
            var coach = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == profile.CoachId, ct);
            coachName = coach is null ? null : $"{coach.FirstName} {coach.LastName}";
        }

        return new ClientUserDetailDto(
            client.Id,
            client.Email,
            client.FirstName,
            client.LastName,
            client.Status.ToString(),
            client.GymId,
            profile?.Id,
            profile?.CoachId,
            coachName);
    }
}
