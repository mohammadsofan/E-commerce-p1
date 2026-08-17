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
    [Route("api/admin/tags")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminTagController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminTagController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all tags (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var query = new GetAdminTagsQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search
            };

            var result = await _queryDispatcher.Send<GetAdminTagsQuery, PagedResult<TagDto>>(query);
            return Ok(result);
        }

        /// <summary>Creates a new tag</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTagCommand command)
        {
            var result = await _commandDispatcher.Send<CreateTagCommand, TagDto>(command);
            return Ok(result);
        }

        /// <summary>Updates an existing tag</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTagCommand command)
        {
            if (id != command.Id)
                return BadRequest("Tag ID mismatch");

            var result = await _commandDispatcher.Send<UpdateTagCommand, TagDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a tag</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteTagCommand { Id = id };
            await _commandDispatcher.Send<DeleteTagCommand, Unit>(command);
            return NoContent();
        }
    }
}