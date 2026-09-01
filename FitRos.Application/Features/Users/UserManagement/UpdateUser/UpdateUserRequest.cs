using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Users.UserManagement.UpdateUser
{
    public sealed record UpdateUserRequest(
      string FirstName,
      string LastName,
      string? Email,
      int? Role
  );
}