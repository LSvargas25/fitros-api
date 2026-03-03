using MediatR;
using System;

namespace FitRos.Application.Features.ClientProfiles.HardDelete
{
    public sealed record HardDeleteClientCommand(Guid ClientId) : IRequest;
}