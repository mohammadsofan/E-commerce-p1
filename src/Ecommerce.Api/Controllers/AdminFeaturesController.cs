using System;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.StoreFeatures;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.StoreFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/features")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminFeaturesController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminFeaturesController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all features with pagination, search, and active filtering</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminFeaturesQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                IsActive = isActive
            };

            var result = await _queryDispatcher.Send<GetAdminFeaturesQuery, PagedResult<StoreFeatureDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a feature by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetFeatureByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetFeatureByIdQuery, StoreFeatureDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new store feature</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStoreFeatureCommand command)
        {
            var result = await _commandDispatcher.Send<CreateStoreFeatureCommand, StoreFeatureDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Updates an existing store feature</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStoreFeatureCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("ID mismatch");
            }

            var result = await _commandDispatcher.Send<UpdateStoreFeatureCommand, StoreFeatureDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a store feature</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteStoreFeatureCommand { Id = id };
            await _commandDispatcher.Send<DeleteStoreFeatureCommand, Unit>(command);
            return NoContent();
        }
    }
}
