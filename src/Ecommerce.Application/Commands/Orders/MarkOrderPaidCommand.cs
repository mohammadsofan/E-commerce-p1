using System;

namespace Ecommerce.Application.Commands.Orders
{
    public class MarkOrderPaidCommand
    {
        public Guid OrderId { get; set; }
    }
}
