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
    [Route("api/admin/products")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminProductController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminProductController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all products (admin view with all details)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? brandId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool includeDeleted = false)
        {
            var query = new GetAdminProductsQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                Status = status,
                BrandId = brandId,
                IsActive = isActive,
                IncludeDeleted = includeDeleted
            };

            var result = await _queryDispatcher.Send<GetAdminProductsQuery, PagedResult<AdminProductDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific product by ID (admin view with all details)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminProductByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminProductByIdQuery, AdminProductDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new product</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
        {
            var product = await _commandDispatcher.Send<CreateProductCommand, AdminProductDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        /// <summary>Updates an existing product</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var product = await _commandDispatcher.Send<UpdateProductCommand, AdminProductDto>(command);
            return Ok(product);
        }

        /// <summary>Deletes a product (soft delete by default)</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] bool hardDelete = false)
        {
            var command = new DeleteProductCommand { Id = id, HardDelete = hardDelete };
            await _commandDispatcher.Send<DeleteProductCommand, Unit>(command);
            return NoContent();
        }
    }
}