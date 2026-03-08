using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Common.Security
{
    public static  class UserPermissions
    {
        public static bool IsOwner(this ICurrentUser user)
       => user.Role == UserRole.OwnerApp;

        public static bool IsAdmin(this ICurrentUser user)
            => user.Role == UserRole.Admin;

        public static bool IsAdminOrOwner(this ICurrentUser user)
            => user.Role == UserRole.Admin
            || user.Role == UserRole.OwnerApp;

        public static bool IsCoach(this ICurrentUser user)
            => user.Role == UserRole.Coach;
    }
}
