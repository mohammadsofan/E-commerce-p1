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
    [Route("api/admin/warehouses")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminWarehouseController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminWarehouseController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all warehouses (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminWarehousesQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                IsActive = isActive
            };

            var result = await _queryDispatcher.Send<GetAdminWarehousesQuery, PagedResult<WarehouseDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific warehouse by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminWarehouseByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminWarehouseByIdQuery, WarehouseDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new warehouse</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseCommand command)
        {
            var result = await _commandDispatcher.Send<CreateWarehouseCommand, WarehouseDto>(command);
            return Ok(result);
        }

        /// <summary>Updates an existing warehouse</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseCommand command)
        {
            command.Id = id;
            var result = await _commandDispatcher.Send<UpdateWarehouseCommand, WarehouseDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a warehouse</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteWarehouseCommand { Id = id };
            await _commandDispatcher.Send<DeleteWarehouseCommand, Unit>(command);
            return NoContent();
        }
    }
}
