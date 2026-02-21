using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Users.GetUsersAdvanced;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // 🔐 Solo Admin puede cambiar email
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

            // Coach puede cambiar nombre/apellido
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