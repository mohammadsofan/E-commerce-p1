using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class ProcessOrderRefundCommand
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public required string Reason { get; set; }
    }
}