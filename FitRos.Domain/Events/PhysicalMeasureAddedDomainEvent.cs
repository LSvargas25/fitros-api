using FitRos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace FitRos.Domain.Events
{
    public sealed class PhysicalMeasureAddedDomainEvent : DomainEvent
    {
        public Guid ClientProfileId { get; }
        public Guid MeasureId { get; }

        public PhysicalMeasureAddedDomainEvent(Guid clientProfileId, Guid measureId)
        {
            ClientProfileId = clientProfileId;
            MeasureId = measureId;
        }
    }
}
