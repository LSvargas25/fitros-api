using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
namespace FitRos.Domain.Common
{
    public abstract class AggregateRoot
    {
        private readonly List<INotification> _domainEvents = new();

        public IReadOnlyCollection<INotification> DomainEvents => _domainEvents;

        protected void AddDomainEvent(INotification eventItem)
            => _domainEvents.Add(eventItem);

        public void ClearDomainEvents()
            => _domainEvents.Clear();
    }
}

