using System;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Domain.Entities
{
    /// <summary>
    /// A line item in a shopping cart. Denormalizes a product snapshot (name + price).
    /// </summary>
    public class CartItem
    {
        public Guid Id { get; set; }
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string? ProductName { get; set; }
        public string? SelectedOptions { get; set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>Computed line total; not persisted.</summary>
        public decimal LineTotal => UnitPrice * Quantity;

        public static CartItem Create(Guid cartId, Guid productId, Guid? productVariantId, string? productName, decimal unitPrice, int quantity, string? selectedOptions = null)
        {
            if (quantity <= 0) throw new DomainException("Quantity must be positive");
            if (unitPrice < 0) throw new DomainException("Unit price cannot be negative");

            var now = DateTimeOffset.UtcNow;
            return new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cartId,
                ProductId = productId,
                ProductVariantId = productVariantId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity,
                SelectedOptions = selectedOptions,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public void SetQuantity(int quantity)
        {
            if (quantity <= 0) throw new DomainException("Quantity must be positive");
            Quantity = quantity;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void UpdateUnitPrice(decimal unitPrice)
        {
            if (unitPrice < 0) throw new DomainException("Unit price cannot be negative");
            UnitPrice = unitPrice;
        }
    }
}
