using MediatR;
using System;

namespace FitRos.Application.Features.ClientProfiles.GetById
{
    public sealed record GetClientByIdQuery(Guid ClientId)
        : IRequest<ClientDetailDto>;
}