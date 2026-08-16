using System;

namespace Ecommerce.Domain.Entities
{
    public class SupportTicketMessage
    {
        public Guid Id { get; set; }
        public Guid SupportTicketId { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
