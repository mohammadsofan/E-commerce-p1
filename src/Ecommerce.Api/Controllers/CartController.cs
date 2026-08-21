using System;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public CartController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets the current user's cart (creating an empty one if needed).</summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _queryDispatcher.Send<GetCartQuery, CartDto>(new GetCartQuery());
            return Ok(result);
        }

        /// <summary>Adds a product (or variant) to the current user's cart.</summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
        {
            var command = new AddToCartCommand
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity,
                SelectedOptions = request.SelectedOptions
            };
            var result = await _commandDispatcher.Send<AddToCartCommand, CartDto>(command);
            return Ok(result);
        }

        /// <summary>Updates the quantity of a cart line (quantity <= 0 removes it).</summary>
        [HttpPut("items/{itemId:guid}")]
        public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequest request)
        {
            var command = new UpdateCartItemCommand { CartItemId = itemId, Quantity = request.Quantity };
            var result = await _commandDispatcher.Send<UpdateCartItemCommand, CartDto>(command);
            return Ok(result);
        }

        /// <summary>Removes a single line from the cart.</summary>
        [HttpDelete("items/{itemId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid itemId)
        {
            var command = new RemoveFromCartCommand { CartItemId = itemId };
            var result = await _commandDispatcher.Send<RemoveFromCartCommand, CartDto>(command);
            return Ok(result);
        }

        /// <summary>Removes all lines from the current user's cart.</summary>
        [HttpDelete]
        public async Task<IActionResult> Clear()
        {
            var result = await _commandDispatcher.Send<ClearCartCommand, CartDto>(new ClearCartCommand());
            return Ok(result);
        }
    }

    public class AddToCartRequest
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public string? SelectedOptions { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }
}
