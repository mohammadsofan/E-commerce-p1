using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class SetReorderPointCommand
    {
        public Guid InventoryItemId { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
    }
}