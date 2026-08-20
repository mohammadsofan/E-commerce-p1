using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/promotions")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminPromotionController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminPromotionController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? type = null)
        {
            var query = new GetAdminPromotionsQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                IsActive = isActive,
                Type = type
            };
            var result = await _queryDispatcher.Send<GetAdminPromotionsQuery, PagedResult<AdminPromotionDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminPromotionByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminPromotionByIdQuery, AdminPromotionDto>(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromotionCommand command)
        {
            var promotion = await _commandDispatcher.Send<CreatePromotionCommand, AdminPromotionDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = promotion.Id }, promotion);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromotionCommand command)
        {
            command.Id = id;
            var promotion = await _commandDispatcher.Send<UpdatePromotionCommand, AdminPromotionDto>(command);
            return Ok(promotion);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeletePromotionCommand { Id = id };
            await _commandDispatcher.Send<DeletePromotionCommand, Unit>(command);
            return NoContent();
        }
    }
}