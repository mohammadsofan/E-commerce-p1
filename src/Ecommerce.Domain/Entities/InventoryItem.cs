using System;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Domain.Entities
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public Guid WarehouseId { get; set; }
        public int QuantityOnHand { get; private set; }
        public int QuantityReserved { get; private set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public bool AllowBackorder { get; set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public int Available => QuantityOnHand - QuantityReserved;

        public InventoryItem()
        {
        }

        public InventoryItem(Guid productId, Guid warehouseId, int quantityOnHand = 0, Guid? productVariantId = null)
        {
            Id = Guid.NewGuid();
            ProductId = productId;
            WarehouseId = warehouseId;
            ProductVariantId = productVariantId;
            QuantityOnHand = quantityOnHand;
            QuantityReserved = 0;
            ReorderLevel = 0;
            ReorderQuantity = 0;
            AllowBackorder = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public InventoryItem(Guid productId, Guid warehouseId, Guid? productVariantId, int quantityOnHand, int reorderLevel, int reorderQuantity, bool allowBackorder)
        {
            Id = Guid.NewGuid();
            ProductId = productId;
            WarehouseId = warehouseId;
            ProductVariantId = productVariantId;
            QuantityOnHand = quantityOnHand;
            QuantityReserved = 0;
            ReorderLevel = reorderLevel;
            ReorderQuantity = reorderQuantity;
            AllowBackorder = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Navigation properties
        public Product? Product { get; set; }
        public ProductVariant? ProductVariant { get; set; }
        public Warehouse? Warehouse { get; set; }

        public void AddStock(int quantity)
        {
            if (quantity <= 0) throw new InventoryException("Quantity to add must be positive");
            QuantityOnHand += quantity;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Reserve(int quantity)
        {
            Reserve(quantity, AllowBackorder);
        }

        /// <summary>
        /// Reserves stock. <paramref name="allowBackorder"/> lets the caller pass the effective
        /// backorder policy for the line (product/variant flag OR warehouse flag) so a single
        /// decision is applied consistently across cart validation and checkout.
        /// </summary>
        public void Reserve(int quantity, bool allowBackorder)
        {
            if (quantity <= 0) throw new InventoryException("Quantity to reserve must be positive");

            if (!allowBackorder && !AllowBackorder && Available < quantity)
            {
                throw new InventoryException("Insufficient stock to reserve the requested quantity");
            }

            QuantityReserved += quantity;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Release(int quantity)
        {
            if (quantity <= 0) throw new InventoryException("Quantity to release must be positive");

            if (quantity > QuantityReserved)
            {
                throw new InventoryException("Cannot release more than reserved quantity");
            }

            QuantityReserved -= quantity;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Consumes a previously placed reservation because the goods have physically
        /// left the warehouse: the reservation is dropped AND on-hand stock is reduced
        /// by the same amount. This is the fulfilment counterpart of <see cref="Reserve"/>
        /// and keeps <see cref="Available"/> stable across the transition.
        /// </summary>
        public void ConsumeReservation(int quantity)
        {
            if (quantity <= 0) throw new InventoryException("Quantity to consume must be positive");

            if (quantity > QuantityReserved)
            {
                throw new InventoryException("Cannot consume more than the reserved quantity");
            }

            QuantityReserved -= quantity;
            QuantityOnHand -= quantity;
            // Backordered lines may legitimately drive on-hand below zero conceptually;
            // clamp so the persisted quantity never becomes negative.
            if (QuantityOnHand < 0) QuantityOnHand = 0;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RemoveStock(int quantity)
        {
            if (quantity <= 0) throw new InventoryException("Quantity to remove must be positive");

            if (!AllowBackorder && QuantityOnHand - quantity < 0)
            {
                throw new InventoryException("Insufficient stock to remove the requested quantity");
            }

            QuantityOnHand -= quantity;
            if (QuantityOnHand < 0) QuantityOnHand = 0;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void SetReorderPoint(int reorderLevel, int reorderQuantity)
        {
            if (reorderLevel < 0) throw new InventoryException("Reorder level cannot be negative");
            if (reorderQuantity < 0) throw new InventoryException("Reorder quantity cannot be negative");

            ReorderLevel = reorderLevel;
            ReorderQuantity = reorderQuantity;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void SetStock(int quantity)
        {
            if (quantity < 0) throw new InventoryException("Quantity cannot be negative");
            QuantityOnHand = quantity;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
