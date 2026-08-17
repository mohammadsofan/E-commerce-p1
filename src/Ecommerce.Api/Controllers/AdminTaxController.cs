using System;
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
    [Route("api/admin/tax")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminTaxController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminTaxController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminTaxCategoriesQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                IsActive = isActive
            };
            var result = await _queryDispatcher.Send<GetAdminTaxCategoriesQuery, PagedResult<AdminTaxCategoryDto>>(query);
            return Ok(result);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateTaxCategoryCommand command)
        {
            var category = await _commandDispatcher.Send<CreateTaxCategoryCommand, AdminTaxCategoryDto>(command);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
        }

        [HttpGet("categories/{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var query = new GetAdminTaxCategoryByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminTaxCategoryByIdQuery, AdminTaxCategoryDto>(query);
            return Ok(result);
        }

        [HttpPut("categories/{id:guid}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateTaxCategoryCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var category = await _commandDispatcher.Send<UpdateTaxCategoryCommand, AdminTaxCategoryDto>(command);
            return Ok(category);
        }

        [HttpDelete("categories/{id:guid}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var command = new DeleteTaxCategoryCommand { Id = id };
            await _commandDispatcher.Send<DeleteTaxCategoryCommand, Unit>(command);
            return NoContent();
        }

        [HttpGet("rates")]
        public async Task<IActionResult> GetRates(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] string? countryCode = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminTaxRatesQuery
            {
                Page = page,
                PageSize = pageSize,
                TaxCategoryId = categoryId,
                CountryCode = countryCode,
                IsActive = isActive
            };
            var result = await _queryDispatcher.Send<GetAdminTaxRatesQuery, PagedResult<AdminTaxRateDto>>(query);
            return Ok(result);
        }

        [HttpPost("rates")]
        public async Task<IActionResult> CreateRate([FromBody] CreateTaxRateOnlyCommand command)
        {
            var rate = await _commandDispatcher.Send<CreateTaxRateOnlyCommand, AdminTaxRateDto>(command);
            return CreatedAtAction(nameof(GetRateById), new { id = rate.Id }, rate);
        }

        [HttpGet("rates/{id:guid}")]
        public async Task<IActionResult> GetRateById(Guid id)
        {
            var query = new GetAdminTaxRateByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminTaxRateByIdQuery, AdminTaxRateDto>(query);
            return Ok(result);
        }

        [HttpPut("rates/{id:guid}")]
        public async Task<IActionResult> UpdateRate(Guid id, [FromBody] UpdateTaxRateOnlyCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var rate = await _commandDispatcher.Send<UpdateTaxRateOnlyCommand, AdminTaxRateDto>(command);
            return Ok(rate);
        }

        [HttpDelete("rates/{id:guid}")]
        public async Task<IActionResult> DeleteRate(Guid id)
        {
            var command = new DeleteTaxRateCommand { Id = id };
            await _commandDispatcher.Send<DeleteTaxRateCommand, Unit>(command);
            return NoContent();
        }
    }
}