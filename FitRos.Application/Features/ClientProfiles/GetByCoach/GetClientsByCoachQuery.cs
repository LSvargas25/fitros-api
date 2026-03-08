using MediatR;
using System;
using System.Collections.Generic;

namespace FitRos.Application.Features.ClientProfiles.GetByCoach
{
    public sealed record GetClientsByCoachQuery(Guid CoachId)
        : IRequest<IReadOnlyCollection<ClientListItemDto>>;
}