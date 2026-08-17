using System;

namespace Ecommerce.Application.Commands.Orders
{
    public class CancelOrderCommand
    {
        public Guid OrderId { get; set; }
        public string? Reason { get; set; }
    }
}
