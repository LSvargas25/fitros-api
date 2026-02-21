using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid Id);