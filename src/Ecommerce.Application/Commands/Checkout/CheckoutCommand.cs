using System;
using System.Collections.Generic;

namespace Ecommerce.Application.Commands.Checkout
{
    public class CheckoutCommand
    {
        public Guid UserId { get; set; }
        public List<CheckoutItem> Items { get; set; } = new List<CheckoutItem>();
        public string Currency { get; set; } = "USD";
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal ShippingAmount { get; set; } = 0m;
        public string? CouponCode { get; set; }
        public string? CustomerNotes { get; set; }
        public string? PaymentMethod { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public class CheckoutItem
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }
}
