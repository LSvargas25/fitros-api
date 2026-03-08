using MediatR;
using System;

namespace FitRos.Application.Features.ClientProfiles.ReassignCoach
{
    public sealed record ReassignClientCoachCommand(
        Guid ClientId,
        Guid NewCoachId
    ) : IRequest;
}