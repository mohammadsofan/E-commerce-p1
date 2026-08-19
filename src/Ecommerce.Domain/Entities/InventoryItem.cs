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
            if (quantity <= 0) throw new InventoryException("Quantity to reserve must be positive");

            if (!AllowBackorder && Available < quantity)
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
