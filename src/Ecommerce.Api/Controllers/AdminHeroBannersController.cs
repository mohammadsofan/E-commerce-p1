using System;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.HeroBanners;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.HeroBanners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/hero-banners")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminHeroBannersController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminHeroBannersController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all hero banners with pagination, search, and active filtering</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminHeroBannersQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                IsActive = isActive
            };

            var result = await _queryDispatcher.Send<GetAdminHeroBannersQuery, PagedResult<HeroBannerDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a hero banner by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetHeroBannerByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetHeroBannerByIdQuery, HeroBannerDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new hero banner</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHeroBannerCommand command)
        {
            var result = await _commandDispatcher.Send<CreateHeroBannerCommand, HeroBannerDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Updates an existing hero banner</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHeroBannerCommand command)
        {
            command.Id = id;
            var result = await _commandDispatcher.Send<UpdateHeroBannerCommand, HeroBannerDto>(command);
            return Ok(result);
        }

        /// <summary>Reorders hero banners</summary>
        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder([FromBody] ReorderHeroBannersCommand command)
        {
            await _commandDispatcher.Send<ReorderHeroBannersCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Sets a hero banner as active primary home banner</summary>
        [HttpPut("{id:guid}/activate")]
        public async Task<IActionResult> SetActive(Guid id)
        {
            var command = new SetActiveHeroBannerCommand { Id = id };
            var result = await _commandDispatcher.Send<SetActiveHeroBannerCommand, HeroBannerDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a hero banner</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteHeroBannerCommand { Id = id };
            await _commandDispatcher.Send<DeleteHeroBannerCommand, Unit>(command);
            return NoContent();
        }
    }
}
