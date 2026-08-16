using System;

namespace Ecommerce.Domain.DomainEvents
{
    public class OrderPlacedDomainEvent : IDomainEvent
    {
        public Guid OrderId { get; }
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

        public OrderPlacedDomainEvent(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
