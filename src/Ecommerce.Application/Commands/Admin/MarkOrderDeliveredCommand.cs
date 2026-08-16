using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class MarkOrderDeliveredCommand
    {
        public Guid OrderId { get; set; }
    }
}