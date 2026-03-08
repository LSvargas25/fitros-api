using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Abstractions.Security
{
    public static class UserQueryExtensions
    {
        public static IQueryable<User> ApplyUserVisibility(
            this IQueryable<User> query,
            ICurrentUser currentUser)
        {
            if (!currentUser.IsAuthenticated)
                throw new UnauthorizedException("User not authenticated.");

            if (currentUser.IsOwner())
                return query;

            if (currentUser.IsAdmin())
                return query.Where(u => u.Role != UserRole.OwnerApp);

            if (currentUser.IsCoach())
                return query.Where(u => u.Role == UserRole.Client);

            return query.Where(u => u.Id == currentUser.UserId);
        }
    }
}
