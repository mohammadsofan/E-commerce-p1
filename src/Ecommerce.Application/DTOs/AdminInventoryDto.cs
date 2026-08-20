using System;

namespace Ecommerce.Application.DTOs
{
    public class AdminInventoryDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int Available => QuantityOnHand - QuantityReserved;
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public bool AllowBackorder { get; set; }
        public bool IsLowStock => Available <= ReorderLevel && ReorderLevel > 0;
        public DateTimeOffset UpdatedAt { get; set; }
    }
}