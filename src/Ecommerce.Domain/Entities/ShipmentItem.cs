using System;

namespace Ecommerce.Domain.Entities
{
    public class ShipmentItem
    {
        public Guid Id { get; set; }
        public Guid ShipmentId { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid InventoryItemId { get; set; }
        public int Quantity { get; set; }
    }
}
