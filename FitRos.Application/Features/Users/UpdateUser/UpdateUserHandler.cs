using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Users.GetUsersAdvanced;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.UpdateUser
{
    public sealed class UpdateUserHandler
        : IRequestHandler<UpdateUserCommand, UserListItemResponse>
    {
        private readonly IFitRosDbContext _context;
        private readonly ICurrentUser _currentUser;

        public UpdateUserHandler(
            IFitRosDbContext context,
            ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<UserListItemResponse> Handle(
            UpdateUserCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (user is null)
                throw new InvalidOperationException("User not found.");

   
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (_currentUser.Role != UserRole.Admin)
                    throw new UnauthorizedAccessException("Only Admin can change email.");

                var normalized = request.Email.Trim().ToUpperInvariant();

                var exists = await _context.Users
                    .AnyAsync(u => u.NormalizedEmail == normalized && u.Id != user.Id,
                              cancellationToken);

                if (exists)
                    throw new InvalidOperationException("Email already in use.");

                user.ChangeEmail(request.Email);
            }

           
            if (request.Role.HasValue && request.Role.Value != user.Role)
            {
                if (_currentUser.Role != UserRole.Admin)
                    throw new UnauthorizedAccessException("Only Admin can change roles.");

 
                if (user.Role == UserRole.Admin &&
                    request.Role.Value != UserRole.Admin)
                {
                    var adminCount = await _context.Users
                        .IgnoreQueryFilters()
                        .CountAsync(u => u.Role == UserRole.Admin, cancellationToken);

                    if (adminCount == 1)
                        throw new InvalidOperationException("Cannot downgrade the last Admin.");
                }

 
                var hasSessions = await _context.WorkoutSessions
                    .AnyAsync(s => s.UserId == user.Id, cancellationToken);

                if (hasSessions)
                    throw new InvalidOperationException("User with sessions cannot change role.");

                user.ChangeRole(request.Role.Value);
            }

            
            user.UpdateBasicInfo(request.FirstName, request.LastName);

            await _context.SaveChangesAsync(cancellationToken);

            return new UserListItemResponse(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                (int)user.Role,
                (int)user.Status,
                user.CreatedAt);
        }
    }
}