using System;

namespace Ecommerce.Application.Commands.Carts
{
    public class AddToCartCommand
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }
}
