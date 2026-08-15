using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class Cart
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string SessionId { get; set; }
        public string CurrencyCode { get; set; }
        public string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
