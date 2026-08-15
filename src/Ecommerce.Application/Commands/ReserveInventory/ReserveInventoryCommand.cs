using System;

namespace Ecommerce.Application.Commands.ReserveInventory
{
    public class ReserveInventoryCommand
    {
        public Guid InventoryItemId { get; set; }
        public int Quantity { get; set; }
    }
}
