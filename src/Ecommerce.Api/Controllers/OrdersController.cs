using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Orders;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Application.Queries.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;
        private readonly CommandDispatcher _commandDispatcher;

        public OrdersController(QueryDispatcher queryDispatcher, CommandDispatcher commandDispatcher)
        {
            _queryDispatcher = queryDispatcher;
            _commandDispatcher = commandDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = new GetOrdersQuery { Page = page, PageSize = pageSize };
            var result = await _queryDispatcher.Send<GetOrdersQuery, List<OrderDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetOrderByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetOrderByIdQuery, OrderDto>(query);
            return Ok(result);
        }

        /// <summary>
        /// Transitions an order from Placed to Paid.
        /// </summary>
        [HttpPost("{id:guid}/pay")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> MarkPaid(Guid id)
        {
            var command = new MarkOrderPaidCommand { OrderId = id };
            var result = await _commandDispatcher.Send<MarkOrderPaidCommand, OrderDto>(command);
            return Ok(result);
        }

        /// <summary>
        /// Transitions a Paid order to Completed.
        /// </summary>
        [HttpPost("{id:guid}/complete")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Complete(Guid id)
        {
            var command = new CompleteOrderCommand { OrderId = id };
            var result = await _commandDispatcher.Send<CompleteOrderCommand, OrderDto>(command);
            return Ok(result);
        }

        /// <summary>
        /// Cancels an order that is not in a terminal state.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest? request)
        {
            var command = new CancelOrderCommand { OrderId = id, Reason = request?.Reason };
            var result = await _commandDispatcher.Send<CancelOrderCommand, OrderDto>(command);
            return Ok(result);
        }

        /// <summary>
        /// Gets the latest shipment for an order (customer order tracking).
        /// </summary>
        [HttpGet("{id:guid}/shipment")]
        public async Task<IActionResult> GetShipment(Guid id)
        {
            var query = new GetOrderShipmentQuery { OrderId = id };
            var result = await _queryDispatcher.Send<GetOrderShipmentQuery, ShipmentDto>(query);
            return Ok(result);
        }
    }

    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }
}
