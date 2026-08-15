using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public Guid? UserId { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string FulfillmentStatus { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public string CouponCode { get; set; }
        public string Notes { get; set; }
        public string CustomerNotes { get; set; }
        public DateTimeOffset? PlacedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
