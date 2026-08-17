using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class ReturnRequest
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ProcessedAt { get; set; }

        public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
    }
}
