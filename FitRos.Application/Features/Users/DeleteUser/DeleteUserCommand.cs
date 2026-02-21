using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Users.DeleteUser
{
    public sealed record DeleteUserCommand(Guid UserId);
}
