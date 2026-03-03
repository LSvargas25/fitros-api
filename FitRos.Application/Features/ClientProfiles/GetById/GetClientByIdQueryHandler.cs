using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.ClientProfiles.GetById;

public sealed class GetClientByIdQueryHandler
    : IRequestHandler<GetClientByIdQuery, ClientDetailDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientByIdQueryHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ClientDetailDto> Handle(
        GetClientByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var client = await _context.ClientProfiles
            .IgnoreQueryFilters()
            .Include(x => x.Measures)
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientId,
                cancellationToken);

        if (client is null)
            throw new NotFoundException("Client not found.");

        ValidatePermissions(client);

        return new ClientDetailDto
        {
            Id = client.Id,
            UserId = client.UserId,
            CoachId = client.CoachId,
            Status = client.Status,
            CreatedAt = client.CreatedAt,
            Measures = client.Measures
                .Select(m => new PhysicalMeasureDto
                {
                    Id = m.Id,
                    Weight = m.Weight,
                    BodyFatPercentage = m.BodyFatPercentage,
                    MuscleMass = m.MuscleMass,
                    Waist = m.Waist,
                    Chest = m.Chest,
                    Arms = m.Arms,
                    RecordedAt = m.RecordedAt
                })
                .ToList()
        };
    }

    private void ValidatePermissions(Domain.Entities.Client.ClientProfile client)
    {
        if (_currentUser.IsOwner() || _currentUser.IsAdmin())
            return;

        if (_currentUser.IsCoach())
        {
            if (client.CoachId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this client.");

            return;
        }

        if (_currentUser.Role == Domain.Enums.UserRole.Client)
        {
            if (client.UserId != _currentUser.UserId)
                throw new ForbiddenException("You are not authorized.");

            return;
        }

        throw new ForbiddenException("You are not authorized.");
    }
}