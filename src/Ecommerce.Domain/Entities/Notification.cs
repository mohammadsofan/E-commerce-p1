using System;

namespace Ecommerce.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Type { get; set; }
        public string DataJson { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
