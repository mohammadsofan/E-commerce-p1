using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class MarkOrderShippedCommand
    {
        public Guid OrderId { get; set; }
        public required string TrackingNumber { get; set; }
        public required string Carrier { get; set; }
    }
}