using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminAuditLogsQuery : IQuery<PagedResult<AuditLogDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? EntityName { get; set; }
        public string? Action { get; set; }
        public Guid? UserId { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
    }

    public class GetAdminAuditLogByIdQuery : IQuery<AuditLogDto>
    {
        public Guid Id { get; set; }
    }
}