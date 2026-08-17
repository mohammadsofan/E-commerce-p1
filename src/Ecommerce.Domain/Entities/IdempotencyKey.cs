using System;

namespace Ecommerce.Domain.Entities
{
    public class IdempotencyKey
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string RequestHash { get; set; } = string.Empty;
        public Guid? OwnerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? ResponseData { get; set; }
    }
}
