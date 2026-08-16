using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class TransferInventoryCommand
    {
        public Guid InventoryItemId { get; set; }
        public Guid FromWarehouseId { get; set; }
        public Guid ToWarehouseId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}