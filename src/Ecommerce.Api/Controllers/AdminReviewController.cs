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
    [Route("api/admin/reviews")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminReviewController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminReviewController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all reviews (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? productId = null,
            [FromQuery] bool? isApproved = null,
            [FromQuery] int? minRating = null)
        {
            var query = new GetAdminReviewsQuery
            {
                Page = page,
                PageSize = pageSize,
                ProductId = productId,
                IsApproved = isApproved,
                MinRating = minRating
            };

            var result = await _queryDispatcher.Send<GetAdminReviewsQuery, PagedResult<ProductReviewDto>>(query);
            return Ok(result);
        }

        /// <summary>Approves or rejects a review</summary>
        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateReviewStatusCommand command)
        {
            command.Id = id;
            await _commandDispatcher.Send<UpdateReviewStatusCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Deletes a review</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteReviewCommand { Id = id };
            await _commandDispatcher.Send<DeleteReviewCommand, Unit>(command);
            return NoContent();
        }
    }
}