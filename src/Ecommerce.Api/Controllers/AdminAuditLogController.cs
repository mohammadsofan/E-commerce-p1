using System;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminAuditLogController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public AdminAuditLogController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets audit logs (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? entityName = null,
            [FromQuery] string? action = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTimeOffset? fromDate = null,
            [FromQuery] DateTimeOffset? toDate = null)
        {
            var query = new GetAdminAuditLogsQuery
            {
                Page = page,
                PageSize = pageSize,
                EntityName = entityName,
                Action = action,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _queryDispatcher.Send<GetAdminAuditLogsQuery, PagedResult<AuditLogDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific audit log by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminAuditLogByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminAuditLogByIdQuery, AuditLogDto>(query);
            return Ok(result);
        }
    }
}