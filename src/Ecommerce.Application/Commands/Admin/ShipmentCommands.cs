using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateShipmentCommand : ICommand<ShipmentDto>
    {
        public Guid OrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public List<ShipmentItemCommand> Items { get; set; } = new List<ShipmentItemCommand>();
    }

    public class ShipmentItemCommand
    {
        public Guid OrderItemId { get; set; }
        public Guid InventoryItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateShipmentStatusCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateShipmentTrackingCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
    }
}