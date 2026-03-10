using FitRos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Events
{
 
    public sealed class WorkoutRoutineCreatedDomainEvent : DomainEvent
    {
        public Guid RoutineId { get; }
        public Guid UserId { get; }

        public WorkoutRoutineCreatedDomainEvent(Guid routineId, Guid userId)
        {
            RoutineId = routineId;
            UserId = userId;
        }
    }
}
