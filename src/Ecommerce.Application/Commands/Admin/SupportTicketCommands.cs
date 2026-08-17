using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateSupportTicketCommand : ICommand<SupportTicketDto>
    {
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
    }

    public class ReplySupportTicketCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
    }

    public class UpdateSupportTicketCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public Guid? AssignedToUserId { get; set; }
    }
}