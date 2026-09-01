using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Client.GetClientById;

public sealed class GetClientByIdHandler : IRequestHandler<GetClientByIdQuery, ClientUserDetailDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientByIdHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ClientUserDetailDto?> Handle(
        GetClientByIdQuery request,
        CancellationToken cancellationToken)
    {
        var client = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Id == request.ClientId && u.Role == UserRole.Client,
                cancellationToken);

        if (client is null)
            return null;

        if (_currentUser.IsAdmin() && client.GymId != _currentUser.GymId)
            throw new ForbiddenException("Admins can only view clients within their gym.");

        var profile = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cp => cp.UserId == client.Id, cancellationToken);

        if (_currentUser.IsCoach())
        {
            if (profile is null || profile.CoachId != _currentUser.UserId!.Value)
                throw new ForbiddenException("Coaches can only view their own clients.");
        }

        string? coachName = null;
        if (profile is not null)
        {
            var coach = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == profile.CoachId, cancellationToken);
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
