using System;
using System.Collections.Generic;
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
    [Route("api/support-tickets")]
    [Authorize(Policy = "AdminOrCustomer")]
    public class SupportTicketsController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public SupportTicketsController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets the current user's support tickets</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var result = await _queryDispatcher.Send<GetMySupportTicketsQuery, List<SupportTicketDto>>(new GetMySupportTicketsQuery());
            return Ok(result);
        }

        /// <summary>Gets a support ticket by ID (scoped to the current user)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetSupportTicketByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetSupportTicketByIdQuery, SupportTicketDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new support ticket</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportTicketCommand command)
        {
            var result = await _commandDispatcher.Send<CreateSupportTicketCommand, SupportTicketDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Replies to a support ticket</summary>
        [HttpPost("{id:guid}/reply")]
        public async Task<IActionResult> Reply(Guid id, [FromBody] ReplySupportTicketCommand command)
        {
            if (id != command.Id)
                return BadRequest("Support ticket ID mismatch");

            await _commandDispatcher.Send<ReplySupportTicketCommand, Unit>(command);
            return NoContent();
        }
    }
}