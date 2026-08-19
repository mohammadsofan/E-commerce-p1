using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateInventoryCommand : ICommand<AdminInventoryDto>
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public Guid WarehouseId { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; } = 0;
        public int ReorderQuantity { get; set; } = 0;
        public bool AllowBackorder { get; set; }
    }
}