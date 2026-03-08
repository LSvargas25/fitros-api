using MediatR;
using System;

namespace FitRos.Application.Features.ClientProfiles.SoftDelete
{
    public sealed record SoftDeleteClientCommand(Guid ClientId) : IRequest;
}