using System;

namespace Ecommerce.Domain.Entities
{
    public class ReturnItem
    {
        public Guid Id { get; set; }
        public Guid ReturnRequestId { get; set; }
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
