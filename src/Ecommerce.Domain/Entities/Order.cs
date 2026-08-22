using System;
using System.Collections.Generic;
using System.Linq;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.DomainEvents;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Domain.Entities
{
    public class Order : AggregateRoot
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public OrderStatus Status { get; private set; }
        public PaymentStatus PaymentStatus { get; private set; }
        public FulfillmentStatus FulfillmentStatus { get; private set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal Subtotal { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal ShippingAmount { get; set; }
        public decimal TotalAmount { get; private set; }
        public decimal CartLevelDiscountAmount { get; private set; }
        public string CartLevelPromotionName { get; private set; } = string.Empty;
        public decimal RefundedAmount { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CustomerNotes { get; set; } = string.Empty;
        public DateTimeOffset? PlacedAt { get; private set; }
        public DateTimeOffset? PaidAt { get; private set; }
        public DateTimeOffset? CancelledAt { get; private set; }
        public DateTimeOffset? CompletedAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

        public void AddItem(Guid productId, Guid productVariantId, string productName, decimal unitPrice, int quantity, decimal discount = 0m, string variantName = "", string sku = "", string productImageUrl = "", string? selectedOptions = null)
        {
            if (quantity <= 0) throw new DomainException("Quantity must be positive");
            if (unitPrice < 0) throw new DomainException("Unit price cannot be negative");

            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ProductVariantId = productVariantId,
                ProductName = productName,
                VariantName = !string.IsNullOrWhiteSpace(variantName) ? variantName : (selectedOptions ?? string.Empty),
                Sku = sku,
                UnitPrice = unitPrice,
                Quantity = quantity,
                DiscountAmount = discount,
                ProductImageUrl = productImageUrl,
                SelectedOptions = selectedOptions
            };

            item.TotalAmount = item.UnitPrice * item.Quantity - item.DiscountAmount;

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

        public void SetShippingAmount(decimal shippingAmount)
        {
            ShippingAmount = Math.Max(0m, shippingAmount);
            RecalculateTotals();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ApplyCoupon(string couponCode, decimal discountAmount)
        {
            CouponCode = couponCode;
            DiscountAmount = Math.Max(0m, discountAmount);
            RecalculateTotals();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ApplyCartLevelPromotion(string promotionName, decimal discountAmount)
        {
            CartLevelPromotionName = promotionName;
            CartLevelDiscountAmount = Math.Max(0m, discountAmount);
            RecalculateTotals();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RecalculateTotals()
        {
            Subtotal = Items.Sum(i => Math.Max(0m, Math.Round((i.UnitPrice * i.Quantity) - i.DiscountAmount, 2, MidpointRounding.AwayFromZero)));
            CartLevelDiscountAmount = Math.Max(0m, Math.Min(Subtotal, CartLevelDiscountAmount));
            var afterCartDiscount = Subtotal - CartLevelDiscountAmount;
            DiscountAmount = Math.Max(0m, Math.Min(afterCartDiscount, DiscountAmount));
            TotalAmount = Math.Max(0m, afterCartDiscount - DiscountAmount + ShippingAmount);
        }

        public void PlaceOrder()
        {
            if (!Items.Any()) throw new DomainException("Cannot place an empty order");
            if (Status != OrderStatus.Draft) throw new DomainException("Order has already been placed");

            Status = OrderStatus.Placed;
            PaymentStatus = PaymentStatus.Pending;
            FulfillmentStatus = FulfillmentStatus.Unfulfilled;
            PlacedAt = DateTimeOffset.UtcNow;
            UpdatedAt = PlacedAt.Value;
            if (CreatedAt == default) CreatedAt = PlacedAt.Value;

            // Ensure totals are up to date
            RecalculateTotals();

            AddDomainEvent(new OrderPlacedDomainEvent(Id));
        }

        /// <summary>
        /// Transitions the order to the Paid state. Only a placed order can be paid.
        /// </summary>
        public void MarkPaid()
        {
            if (Status != OrderStatus.Placed) throw new DomainException("Only a placed order can be marked as paid");

            Status = OrderStatus.Paid;
            PaymentStatus = PaymentStatus.Paid;
            PaidAt = DateTimeOffset.UtcNow;
            UpdatedAt = PaidAt.Value;
        }

        /// <summary>
        /// Marks the order as completed. Only a paid order can be completed.
        /// For Cash on Delivery, completing the order confirms payment collection.
        /// </summary>
        public void Complete()
        {
            if (Status != OrderStatus.Paid && (Notes.Contains("CashOnDelivery") || CustomerNotes.Contains("CashOnDelivery")))
            {
                Status = OrderStatus.Paid;
                PaymentStatus = PaymentStatus.Paid;
                PaidAt = DateTimeOffset.UtcNow;
            }

            if (Status != OrderStatus.Paid) throw new DomainException("Only a paid order can be completed");

            Status = OrderStatus.Completed;
            FulfillmentStatus = FulfillmentStatus.Delivered;
            CompletedAt = DateTimeOffset.UtcNow;
            UpdatedAt = CompletedAt.Value;
        }

        /// <summary>
        /// Cancels the order. An order in a terminal state (cancelled/completed/refunded) cannot be cancelled.
        /// </summary>
        public void Cancel(string? reason = null)
        {
            if (Status is OrderStatus.Cancelled or OrderStatus.Completed or OrderStatus.Refunded)
                throw new DomainException("Cannot cancel an order in a terminal state");

            Status = OrderStatus.Cancelled;
            CancelledAt = DateTimeOffset.UtcNow;
            UpdatedAt = CancelledAt.Value;
            if (!string.IsNullOrWhiteSpace(reason)) Notes = reason;
        }

        /// <summary>
        /// Marks the order as shipped with tracking information.
        /// </summary>
        public void MarkShipped(string trackingNumber, string carrier)
        {
            if (Status != OrderStatus.Paid && !Notes.Contains("CashOnDelivery") && !CustomerNotes.Contains("CashOnDelivery"))
                throw new DomainException("Only paid or Cash on Delivery orders can be shipped");
            if (FulfillmentStatus == FulfillmentStatus.Shipped || FulfillmentStatus == FulfillmentStatus.Delivered)
                throw new DomainException("Order is already shipped or delivered");

            FulfillmentStatus = FulfillmentStatus.Shipped;
            UpdatedAt = DateTimeOffset.UtcNow;
            var shipNote = $"Shipped via {carrier} with tracking: {trackingNumber}";
            Notes = string.IsNullOrWhiteSpace(Notes) ? shipNote : $"{Notes} | {shipNote}";
        }

        /// <summary>
        /// Marks the order as delivered.
        /// For Cash on Delivery, marking as delivered automatically marks the payment as collected (Paid).
        /// </summary>
        public void MarkDelivered()
        {
            if (FulfillmentStatus != FulfillmentStatus.Shipped)
                throw new DomainException("Only shipped orders can be marked as delivered");

            FulfillmentStatus = FulfillmentStatus.Delivered;
            UpdatedAt = DateTimeOffset.UtcNow;

            if (Notes.Contains("CashOnDelivery") || CustomerNotes.Contains("CashOnDelivery"))
            {
                Status = OrderStatus.Paid;
                PaymentStatus = PaymentStatus.Paid;
                PaidAt = DateTimeOffset.UtcNow;
            }
        }

        /// <summary>
        /// Processes a full or partial refund.
        /// </summary>
        public void ProcessRefund(decimal amount, string reason)
        {
            if (amount <= 0) throw new DomainException("Refund amount must be positive");
            if (amount > TotalAmount - RefundedAmount)
                throw new DomainException("Refund amount cannot exceed remaining refundable amount");

            RefundedAmount += amount;
            UpdatedAt = DateTimeOffset.UtcNow;

            if (RefundedAmount >= TotalAmount)
            {
                Status = OrderStatus.Refunded;
                PaymentStatus = PaymentStatus.Refunded;
            }
            else
            {
                PaymentStatus = PaymentStatus.PartiallyRefunded;
            }

            Notes = $"Refund of {amount:C}: {reason}";
        }

        /// <summary>
        /// Processes a return request for one or more items.
        /// </summary>
        public void ProcessReturn(IEnumerable<Guid> orderItemIds, string reason)
        {
            if (orderItemIds == null || !orderItemIds.Any())
                throw new DomainException("At least one item must be returned");

            var itemsToReturn = Items.Where(i => orderItemIds.Contains(i.Id)).ToList();
            if (itemsToReturn.Count != orderItemIds.Count())
                throw new DomainException("One or more order items not found");

            if (itemsToReturn.Any(i => i.Quantity <= 0))
                throw new DomainException("Cannot return items with zero quantity");

            // In a real implementation, you'd create ReturnRequest/ReturnItem entities
            // For now, we'll process a refund for the returned items
            var returnAmount = itemsToReturn.Sum(i => i.TotalAmount);
            ProcessRefund(returnAmount, $"Return: {reason}");
        }
    }
}
