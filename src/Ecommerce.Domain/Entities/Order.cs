using System;
using System.Collections.Generic;
using System.Linq;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public Guid? UserId { get; set; }
        public string Status { get; private set; }
        public string PaymentStatus { get; private set; }
        public string FulfillmentStatus { get; private set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal Subtotal { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal ShippingAmount { get; set; }
        public decimal TaxAmount { get; private set; }
        public decimal TotalAmount { get; private set; }
        public decimal RefundedAmount { get; set; }
        public string CouponCode { get; private set; }
        public string Notes { get; set; }
        public string CustomerNotes { get; set; }
        public DateTimeOffset? PlacedAt { get; private set; }
        public DateTimeOffset? PaidAt { get; private set; }
        public DateTimeOffset? CancelledAt { get; private set; }
        public DateTimeOffset? CompletedAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public byte[] RowVersion { get; set; }

        public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

        public void AddItem(Guid productId, Guid productVariantId, string productName, decimal unitPrice, int quantity, decimal discount = 0m, decimal tax = 0m)
        {
            if (quantity <= 0) throw new DomainException("Quantity must be positive");
            if (unitPrice < 0) throw new DomainException("Unit price cannot be negative");

            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ProductVariantId = productVariantId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity,
                DiscountAmount = discount,
                TaxAmount = tax,
            };

            item.TotalAmount = item.UnitPrice * item.Quantity - item.DiscountAmount + item.TaxAmount;

            Items.Add(item);
            RecalculateTotals();
            UpdatedAt = DateTimeOffset.UtcNow;
            if (CreatedAt == default) CreatedAt = UpdatedAt;
        }

        public void RemoveItem(Guid orderItemId)
        {
            var item = Items.FirstOrDefault(i => i.Id == orderItemId);
            if (item == null) throw new DomainException("Order item not found");

            Items.Remove(item);
            RecalculateTotals();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ApplyCoupon(string couponCode, decimal discountAmount)
        {
            CouponCode = couponCode;
            DiscountAmount = discountAmount;
            RecalculateTotals();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RecalculateTotals()
        {
            Subtotal = Items.Sum(i => i.UnitPrice * i.Quantity);
            TaxAmount = Items.Sum(i => i.TaxAmount);
            // DiscountAmount is partially from items and partially from coupon
            var itemsDiscount = Items.Sum(i => i.DiscountAmount);
            DiscountAmount = itemsDiscount + DiscountAmount; // if coupon already set, it will be included
            TotalAmount = Subtotal - DiscountAmount + ShippingAmount + TaxAmount;
        }

        public void PlaceOrder()
        {
            if (!Items.Any()) throw new DomainException("Cannot place an empty order");

            Status = "Placed";
            PaymentStatus = "Pending";
            FulfillmentStatus = "Unfulfilled";
            PlacedAt = DateTimeOffset.UtcNow;
            UpdatedAt = PlacedAt.Value;
            if (CreatedAt == default) CreatedAt = PlacedAt.Value;

            // Ensure totals are up to date
            RecalculateTotals();
        }
    }
}
