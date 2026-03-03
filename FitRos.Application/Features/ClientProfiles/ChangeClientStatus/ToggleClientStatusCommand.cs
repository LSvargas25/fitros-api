

using FitRos.Domain.Entities.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.ClientProfiles.ChangeClientStatus
{
    public sealed record ToggleClientStatusCommand(
     Guid ClientId,
     bool Activate
 ) : IRequest;
}