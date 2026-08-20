using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/products/{productId:guid}/reviews")]
    [Authorize(Policy = "AdminOrCustomer")]
    public class ReviewsController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public ReviewsController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets approved reviews for a product (public view)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid productId)
        {
            var query = new GetProductReviewsQuery { ProductId = productId };
            var result = await _queryDispatcher.Send<GetProductReviewsQuery, List<ProductReviewDto>>(query);
            return Ok(result);
        }

        /// <summary>Submits a new review for a product</summary>
        [HttpPost]
        public async Task<IActionResult> Submit(Guid productId, [FromBody] SubmitProductReviewCommand command)
        {
            command.ProductId = productId;
            var result = await _commandDispatcher.Send<SubmitProductReviewCommand, ProductReviewDto>(command);
            return CreatedAtAction(nameof(GetAll), new { productId }, result);
        }
    }
}