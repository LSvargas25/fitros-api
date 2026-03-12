using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Client.GetClients;

public sealed class GetClientsHandler : IRequestHandler<GetClientsQuery, List<ClientUserListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientsHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ClientUserListItemDto>> Handle(
        GetClientsQuery request,
        CancellationToken cancellationToken)
    {
        var clientsQuery = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Client);

        if (_currentUser.IsAdmin())
            clientsQuery = clientsQuery.Where(u => u.GymId == _currentUser.GymId);

        if (_currentUser.IsCoach())
        {
            var myClientUserIds = _context.ClientProfiles
                .IgnoreQueryFilters()
                .Where(cp => cp.CoachId == _currentUser.UserId)
                .Select(cp => cp.UserId);
            clientsQuery = clientsQuery.Where(u => myClientUserIds.Contains(u.Id));
        }

        var result = await clientsQuery
            .Select(client => new ClientUserListItemDto(
                client.Id,
                client.Email,
                client.FirstName,
                client.LastName,
                client.Status.ToString(),
                client.GymId))
            .ToListAsync(cancellationToken);

        return result;
    }
}