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
    [Route("api/admin/shipments")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminShipmentController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminShipmentController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all shipments (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? orderId = null,
            [FromQuery] string? status = null)
        {
            var query = new GetAdminShipmentsQuery
            {
                Page = page,
                PageSize = pageSize,
                OrderId = orderId,
                Status = status
            };

            var result = await _queryDispatcher.Send<GetAdminShipmentsQuery, PagedResult<ShipmentDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific shipment by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminShipmentByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminShipmentByIdQuery, ShipmentDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new shipment</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateShipmentCommand command)
        {
            var result = await _commandDispatcher.Send<CreateShipmentCommand, ShipmentDto>(command);
            return Ok(result);
        }

        /// <summary>Updates the status of a shipment</summary>
        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateShipmentStatusCommand command)
        {
            command.Id = id;
            await _commandDispatcher.Send<UpdateShipmentStatusCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Updates the carrier and tracking number of a shipment</summary>
        [HttpPut("{id:guid}/tracking")]
        public async Task<IActionResult> UpdateTracking(Guid id, [FromBody] UpdateShipmentTrackingCommand command)
        {
            command.Id = id;
            await _commandDispatcher.Send<UpdateShipmentTrackingCommand, Unit>(command);
            return NoContent();
        }
    }
}