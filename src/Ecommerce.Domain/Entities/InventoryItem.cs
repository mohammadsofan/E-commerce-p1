using System;

namespace Ecommerce.Domain.Entities
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid WarehouseId { get; set; }
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public bool AllowBackorder { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; }
    }
}
