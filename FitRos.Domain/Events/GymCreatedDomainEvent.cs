using FitRos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Events
{
    // GymCreatedDomainEvent.cs
    public sealed class GymCreatedDomainEvent : DomainEvent
    {
        public Guid GymId { get; }
        public string Name { get; }

        public GymCreatedDomainEvent(Guid gymId, string name)
        {
            GymId = gymId;
            Name = name;
        }
    }
}
