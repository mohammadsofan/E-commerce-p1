using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class ReturnRequest
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ProcessedAt { get; set; }

        public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
    }
}
