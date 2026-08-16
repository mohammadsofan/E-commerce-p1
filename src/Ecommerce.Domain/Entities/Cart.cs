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
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public DateTimeOffset? ExpiresAt { get; set; }

        public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

        /// <summary>Computed cart total; not persisted.</summary>
        public decimal TotalAmount => Items.Sum(i => i.LineTotal);

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

        public void AddItem(Guid productId, Guid? productVariantId, string? productName, decimal unitPrice, int quantity)
        {
            if (quantity <= 0) throw new DomainException("Quantity must be positive");

            var existing = Items.FirstOrDefault(i => i.ProductId == productId && i.ProductVariantId == productVariantId);
            if (existing != null)
            {
                existing.SetQuantity(existing.Quantity + quantity);
                existing.UpdateUnitPrice(unitPrice);
            }
            else
            {
                Items.Add(CartItem.Create(Id, productId, productVariantId, productName, unitPrice, quantity));
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
            Touch();
        }

        public void MarkOrdered()
        {
            Status = CartStatus.Ordered;
            Touch();
        }

        public void Abandon()
        {
            Status = CartStatus.Abandoned;
            Touch();
        }

        private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
    }
}
