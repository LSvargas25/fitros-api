using FitRos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Events
{
    public sealed class ClientProfileCreatedDomainEvent : DomainEvent
    {
        public Guid ClientProfileId { get; }
        public Guid GymId { get; }
        public ClientProfileCreatedDomainEvent(Guid clientProfileId, Guid gymId)
        { ClientProfileId = clientProfileId; GymId = gymId; }
    }
}
