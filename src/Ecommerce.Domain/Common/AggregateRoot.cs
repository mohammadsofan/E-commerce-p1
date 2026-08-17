using System.Collections.Generic;
using Ecommerce.Domain.DomainEvents;

namespace Ecommerce.Domain.Common
{
    /// <summary>
    /// Base for aggregate roots that collect domain events to be dispatched after persistence.
    /// </summary>
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent is null) return;
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
