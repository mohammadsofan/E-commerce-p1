using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Wishlist;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Wishlist;
using Ecommerce.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public WishlistController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets the current user's wishlist items.</summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _queryDispatcher.Send<GetWishlistQuery, List<WishlistItemDto>>(new GetWishlistQuery());
            return Ok(result);
        }

        /// <summary>Adds a product to the current user's wishlist.</summary>
        [HttpPost("items")]
        [ValidateCustomCsrf]
        public async Task<IActionResult> AddItem([FromBody] AddToWishlistRequest request)
        {
            var command = new AddToWishlistCommand
            {
                ProductId = request.ProductId
            };
            var result = await _commandDispatcher.Send<AddToWishlistCommand, WishlistItemDto>(command);
            return Ok(result);
        }

        /// <summary>Removes a product from the current user's wishlist.</summary>
        [HttpDelete("items/{productId:guid}")]
        [ValidateCustomCsrf]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            var command = new RemoveFromWishlistCommand { ProductId = productId };
            await _commandDispatcher.Send<RemoveFromWishlistCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Clears the current user's wishlist.</summary>
        [HttpDelete]
        [ValidateCustomCsrf]
        public async Task<IActionResult> Clear()
        {
            await _commandDispatcher.Send<ClearWishlistCommand, Unit>(new ClearWishlistCommand());
            return NoContent();
        }
    }

    public class AddToWishlistRequest
    {
        public Guid ProductId { get; set; }
    }
}

