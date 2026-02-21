using FitRos.Application.Features.Users.GetUsersAdvanced;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Users.UpdateUser
{
    public sealed record UpdateUserCommand(
     Guid Id,
     string FirstName,
     string LastName,
     string? Email
 ) : IRequest<UserListItemResponse>;
}