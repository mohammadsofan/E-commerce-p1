using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class Shipment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; }
        public string Status { get; set; }
        public DateTimeOffset? ShippedAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    }
}
