using System;
using System.Collections.Generic;
using System.Linq;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Domain.Entities
{
    /// <summary>
    /// Shopping cart aggregate root for a user (or anonymous session).
    /// Manages its own line items and exposes a computed total.
    /// </summary>
    public class Cart
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string? SessionId { get; set; }
        public required string CurrencyCode { get; set; } = "USD";
        public CartStatus Status { get; private set; }
        public string? AppliedCouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public DateTimeOffset? ExpiresAt { get; set; }

        public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

        /// <summary>Subtotal of all line items before discounts; not persisted.</summary>
        public decimal Subtotal => Items.Sum(i => i.LineTotal);

        /// <summary>Computed cart total after discount; not persisted. Never drops below zero.</summary>
        public decimal TotalAmount => Math.Max(0m, Subtotal - DiscountAmount);

        public static Cart Create(Guid? userId, string? sessionId, string? currencyCode = null)
        {
            var now = DateTimeOffset.UtcNow;
            return new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                CurrencyCode = currencyCode ?? "USD",
                Status = CartStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                ExpiresAt = now.AddDays(30)
            };
        }

        public void AddItem(Guid productId, Guid? productVariantId, string? productName, decimal unitPrice, int quantity, string? selectedOptions = null)
        {
            if (quantity <= 0) throw new DomainException("Quantity must be positive");

            var normalizedOptions = string.IsNullOrWhiteSpace(selectedOptions) ? null : selectedOptions.Trim();

            var existing = Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.ProductVariantId == productVariantId &&
                (string.IsNullOrWhiteSpace(i.SelectedOptions) ? null : i.SelectedOptions.Trim()) == normalizedOptions);

            if (existing != null)
            {
                existing.SetQuantity(existing.Quantity + quantity);
                existing.UpdateUnitPrice(unitPrice);
            }
            else
            {
                Items.Add(CartItem.Create(Id, productId, productVariantId, productName, unitPrice, quantity, normalizedOptions));
            }

            Touch();
        }

        public void UpdateItemQuantity(Guid itemId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) throw new DomainException("Cart item not found");

            if (quantity <= 0)
            {
                Items.Remove(item);
            }
            else
            {
                item.SetQuantity(quantity);
            }

            Touch();
        }

        public void RemoveItem(Guid itemId)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) throw new DomainException("Cart item not found");
            Items.Remove(item);
            Touch();
        }

        public void Clear()
        {
            Items.Clear();
            AppliedCouponCode = null;
            DiscountAmount = 0m;
            Touch();
        }

        public void MarkOrdered()
        {
            Status = CartStatus.Ordered;
            AppliedCouponCode = null;
            DiscountAmount = 0m;
            Touch();
        }

        public void Abandon()
        {
            Status = CartStatus.Abandoned;
            Touch();
        }

        public void ApplyCoupon(string couponCode, decimal discountAmount)
        {
            AppliedCouponCode = couponCode.Trim().ToUpperInvariant();
            DiscountAmount = Math.Max(0m, Math.Min(Subtotal, discountAmount));
            Touch();
        }

        public void RemoveCoupon()
        {
            AppliedCouponCode = null;
            DiscountAmount = 0m;
            Touch();
        }

        public decimal CalculateTotals()
        {
            if (Items.Count == 0 || Subtotal <= 0)
            {
                DiscountAmount = 0m;
                AppliedCouponCode = null;
            }
            else
            {
                DiscountAmount = Math.Max(0m, Math.Min(Subtotal, DiscountAmount));
            }
            Touch();
            return TotalAmount;
        }

        private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
    }
}
