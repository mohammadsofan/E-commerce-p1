using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetMySupportTicketsQuery : IQuery<List<SupportTicketDto>>
    {
    }

    public class GetSupportTicketByIdQuery : IQuery<SupportTicketDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminSupportTicketsQuery : IQuery<PagedResult<SupportTicketDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Search { get; set; }
    }

    public class GetAdminSupportTicketByIdQuery : IQuery<SupportTicketDto>
    {
        public Guid Id { get; set; }
    }
}