using System;

namespace Ecommerce.Domain.DomainEvents
{
    public class PaymentCompletedDomainEvent
    {
        public Guid PaymentId { get; }
        public Guid OrderId { get; }
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

        public PaymentCompletedDomainEvent(Guid paymentId, Guid orderId)
        {
            PaymentId = paymentId;
            OrderId = orderId;
        }
    }
}
