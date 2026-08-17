using System;

namespace Ecommerce.Domain.DomainEvents
{
    /// <summary>
    /// Marker for a domain event raised by an aggregate.
    /// </summary>
    public interface IDomainEvent
    {
        DateTimeOffset OccurredAt { get; }
    }
}
