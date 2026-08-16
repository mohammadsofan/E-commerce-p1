using System;

namespace Ecommerce.Application.Commands.Orders
{
    public class CompleteOrderCommand
    {
        public Guid OrderId { get; set; }
    }
}
