using System;

namespace Ecommerce.Application.Commands.Carts
{
    public class UpdateCartItemCommand
    {
        public Guid CartItemId { get; set; }
        public int Quantity { get; set; }
    }
}
