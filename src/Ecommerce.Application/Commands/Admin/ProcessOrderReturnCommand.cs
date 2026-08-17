using System;
using System.Collections.Generic;

namespace Ecommerce.Application.Commands.Admin
{
    public class ProcessOrderReturnCommand
    {
        public Guid OrderId { get; set; }
        public required List<Guid> OrderItemIds { get; set; }
        public required string Reason { get; set; }
    }
}