using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class AdjustInventoryCommand
    {
        public Guid InventoryItemId { get; set; }
        public int QuantityChange { get; set; } // positive for add, negative for remove
        public string Reason { get; set; } = string.Empty;
    }
}