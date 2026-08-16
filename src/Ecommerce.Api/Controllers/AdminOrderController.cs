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
    [Route("api/admin/orders")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminOrderController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminOrderController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all orders (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? paymentStatus = null,
            [FromQuery] string? fulfillmentStatus = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTimeOffset? fromDate = null,
            [FromQuery] DateTimeOffset? toDate = null)
        {
            var query = new GetAdminOrdersQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                Status = status,
                PaymentStatus = paymentStatus,
                FulfillmentStatus = fulfillmentStatus,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _queryDispatcher.Send<GetAdminOrdersQuery, PagedResult<OrderDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific order by ID (admin view with all details)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminOrderByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminOrderByIdQuery, OrderDto>(query);
            return Ok(result);
        }

        /// <summary>Marks an order as shipped with tracking information</summary>
        [HttpPost("{id:guid}/ship")]
        public async Task<IActionResult> Ship(Guid id, [FromBody] MarkOrderShippedCommand command)
        {
            if (id != command.OrderId)
                return BadRequest("Order ID mismatch");

            await _commandDispatcher.Send<MarkOrderShippedCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Marks an order as delivered</summary>
        [HttpPost("{id:guid}/deliver")]
        public async Task<IActionResult> Deliver(Guid id)
        {
            var command = new MarkOrderDeliveredCommand { OrderId = id };
            await _commandDispatcher.Send<MarkOrderDeliveredCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Processes a full or partial refund for an order</summary>
        [HttpPost("{id:guid}/refund")]
        public async Task<IActionResult> Refund(Guid id, [FromBody] ProcessOrderRefundCommand command)
        {
            if (id != command.OrderId)
                return BadRequest("Order ID mismatch");

            await _commandDispatcher.Send<ProcessOrderRefundCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Processes a return for one or more order items</summary>
        [HttpPost("{id:guid}/return")]
        public async Task<IActionResult> Return(Guid id, [FromBody] ProcessOrderReturnCommand command)
        {
            if (id != command.OrderId)
                return BadRequest("Order ID mismatch");

            await _commandDispatcher.Send<ProcessOrderReturnCommand, Unit>(command);
            return NoContent();
        }
    }
}