using FitRos.Domain.Entities.Enums;
using System;

namespace FitRos.Application.Features.ClientProfiles.GetByCoach
{
    public sealed class ClientListItemDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public ClientStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}