using System;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/support-tickets")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSupportTicketController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminSupportTicketController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all support tickets (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] Guid? assignedToUserId = null,
            [FromQuery] string? search = null)
        {
            var query = new GetAdminSupportTicketsQuery
            {
                Page = page,
                PageSize = pageSize,
                Status = status,
                Priority = priority,
                AssignedToUserId = assignedToUserId,
                Search = search
            };

            var result = await _queryDispatcher.Send<GetAdminSupportTicketsQuery, PagedResult<SupportTicketDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific support ticket by ID (admin view with all details)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminSupportTicketByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminSupportTicketByIdQuery, SupportTicketDto>(query);
            return Ok(result);
        }

        /// <summary>Updates a support ticket's status, priority, or assignee</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupportTicketCommand command)
        {
            if (id != command.Id)
                return BadRequest("Support ticket ID mismatch");

            await _commandDispatcher.Send<UpdateSupportTicketCommand, Unit>(command);
            return NoContent();
        }
    }
}