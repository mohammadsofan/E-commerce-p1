using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Products;
using Ecommerce.Application.Queries.Orders;
using Ecommerce.Application.Queries.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public AdminController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all products (admin view with all details)</summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = new GetProductsQuery { Page = page, PageSize = pageSize };
            var result = await _queryDispatcher.Send<GetProductsQuery, List<ProductDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets all orders (admin view)</summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = new GetOrdersQuery { Page = page, PageSize = pageSize };
            var result = await _queryDispatcher.Send<GetOrdersQuery, List<OrderDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific order by ID (admin view)</summary>
        [HttpGet("orders/{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var query = new GetOrderByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetOrderByIdQuery, OrderDto>(query);
            return Ok(result);
        }

        /// <summary>Gets all carts (admin view)</summary>
        [HttpGet("carts")]
        public async Task<IActionResult> GetCarts([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            // This would need a new GetCartsQuery for admin (get all carts, not just current user's)
            // For now, return not implemented
            return StatusCode(501, new { message = "Admin cart listing not yet implemented. Requires GetCartsQuery with admin scope." });
        }

        /// <summary>Gets system health/status</summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0"
        });
    }
}