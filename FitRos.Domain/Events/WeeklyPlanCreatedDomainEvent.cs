using FitRos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Events
{
    // WeeklyPlanCreatedDomainEvent.cs
    public sealed class WeeklyPlanCreatedDomainEvent : DomainEvent
    {
        public Guid PlanId { get; }
        public Guid UserId { get; }

        public WeeklyPlanCreatedDomainEvent(Guid planId, Guid userId)
        {
            PlanId = planId;
            UserId = userId;
        }
    }
}
