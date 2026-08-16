using System;

namespace Ecommerce.Domain.Entities
{
    public class IdempotencyKey
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string RequestHash { get; set; }
        public Guid? OwnerId { get; set; }
        public string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? ResponseData { get; set; }
    }
}
