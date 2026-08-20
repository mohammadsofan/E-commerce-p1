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
    [Route("api/admin/inventory")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminInventoryController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminInventoryController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all inventory items (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] Guid? productId = null,
            [FromQuery] Guid? warehouseId = null,
            [FromQuery] bool? lowStockOnly = null,
            [FromQuery] bool includeBackorder = false)
        {
            var query = new GetAdminInventoryQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                ProductId = productId,
                WarehouseId = warehouseId,
                LowStockOnly = lowStockOnly,
                IncludeBackorder = includeBackorder
            };

            var result = await _queryDispatcher.Send<GetAdminInventoryQuery, PagedResult<AdminInventoryDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific inventory item by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminInventoryByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminInventoryByIdQuery, AdminInventoryDto>(query);
            return Ok(result);
        }

        /// <summary>Sets inventory quantity to an absolute value</summary>
        [HttpPost("set-stock")]
        public async Task<IActionResult> SetStock([FromBody] SetInventoryStockCommand command)
        {
            var result = await _commandDispatcher.Send<SetInventoryStockCommand, AdminInventoryDto>(command);
            return Ok(result);
        }

        /// <summary>Creates an inventory item for a product or variant in a warehouse</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInventoryCommand command)
        {
            var result = await _commandDispatcher.Send<CreateInventoryCommand, AdminInventoryDto>(command);
            return Ok(result);
        }

        /// <summary>Adjusts inventory quantity (positive to add, negative to remove)</summary>
        [HttpPost("{id:guid}/adjust")]
        public async Task<IActionResult> Adjust(Guid id, [FromBody] AdjustInventoryCommand command)
        {
            command.InventoryItemId = id;
            await _commandDispatcher.Send<AdjustInventoryCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Transfers inventory between warehouses</summary>
        [HttpPost("{id:guid}/transfer")]
        public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferInventoryCommand command)
        {
            command.InventoryItemId = id;
            await _commandDispatcher.Send<TransferInventoryCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Sets reorder point and quantity for an inventory item</summary>
        [HttpPut("{id:guid}/reorder-point")]
        public async Task<IActionResult> SetReorderPoint(Guid id, [FromBody] SetReorderPointCommand command)
        {
            command.InventoryItemId = id;
            await _commandDispatcher.Send<SetReorderPointCommand, Unit>(command);
            return NoContent();
        }
    }
}
