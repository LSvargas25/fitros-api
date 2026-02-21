using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Users.ActivateUser
{
    public sealed record ActivateUserCommand(Guid UserId);
}
