using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class SetInventoryStockCommand : ICommand<AdminInventoryDto>
    {
        public Guid InventoryItemId { get; set; }
        public int QuantityOnHand { get; set; }
    }
}