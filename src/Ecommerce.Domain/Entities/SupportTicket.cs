using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class SupportTicket
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
    }
}
