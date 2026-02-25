using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Users.CreateUser
{
    public sealed record CreateClientCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
) : IRequest<CreateUserResponse>;
}
 