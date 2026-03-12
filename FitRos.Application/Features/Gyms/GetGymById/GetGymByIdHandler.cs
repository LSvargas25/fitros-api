using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.GetGymById;

public sealed class GetGymByIdHandler : IRequestHandler<GetGymByIdQuery, GymDetailDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetGymByIdHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<GymDetailDto?> Handle(
        GetGymByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role == UserRole.Admin &&
            _currentUser.GymId != request.GymId)
            throw new ForbiddenException("Admins can only query their own gym.");

        var gym = await _context.Gyms
            .FirstOrDefaultAsync(g => g.Id == request.GymId, cancellationToken);

        if (gym is null)
            return null;

        var admin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Role == UserRole.Admin && u.GymId == gym.Id,
                cancellationToken);

        return new GymDetailDto(
            gym.Id,
            gym.Name,
            gym.Address,
            gym.PhoneNumber,
            gym.IsActive,
            gym.IsDeleted,
            admin?.Id,
            admin?.Email,
            admin is null ? null : $"{admin.FirstName} {admin.LastName}");
    }
}
