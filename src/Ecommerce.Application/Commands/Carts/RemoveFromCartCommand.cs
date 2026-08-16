using System;

namespace Ecommerce.Application.Commands.Carts
{
    public class RemoveFromCartCommand
    {
        public Guid CartItemId { get; set; }
    }
}
