using FitRos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Events
{ 
    public sealed class UserRegisteredDomainEvent : DomainEvent
    {
        public Guid UserId { get; }
        public string Email { get; }

        public UserRegisteredDomainEvent(Guid userId, string email)
        {
            UserId = userId;
            Email = email;
        }
    }
 
}
