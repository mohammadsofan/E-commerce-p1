using System;
using System.Collections.Generic;
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
    [Route("api/admin/products/{productId:guid}/variants")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminProductVariantController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminProductVariantController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all variants for a product (admin view with all details)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            Guid productId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminProductVariantsQuery
            {
                ProductId = productId,
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                IsActive = isActive
            };

            var result = await _queryDispatcher.Send<GetAdminProductVariantsQuery, PagedResult<AdminProductVariantDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific variant by ID (admin view with all details)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid productId, Guid id)
        {
            var query = new GetAdminProductVariantByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminProductVariantByIdQuery, AdminProductVariantDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new product variant</summary>
        [HttpPost]
        public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductVariantCommand command)
        {
            if (productId != command.ProductId)
                return BadRequest("Product ID mismatch");

            var variant = await _commandDispatcher.Send<CreateProductVariantCommand, AdminProductVariantDto>(command);
            return CreatedAtAction(nameof(GetById), new { productId, id = variant.Id }, variant);
        }

        /// <summary>Updates an existing product variant</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductVariantCommand command)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var variant = await _commandDispatcher.Send<UpdateProductVariantCommand, AdminProductVariantDto>(command);
            return Ok(variant);
        }

        /// <summary>Deletes a product variant</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid productId, Guid id)
        {
            var command = new DeleteProductVariantCommand { Id = id };
            await _commandDispatcher.Send<DeleteProductVariantCommand, Unit>(command);
            return NoContent();
        }
    }

    [ApiController]
    [Route("api/admin/products/{productId:guid}/images")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminProductImageController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminProductImageController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all images for a product (or variant)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            Guid productId,
            [FromQuery] Guid? productVariantId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetAdminProductImagesQuery
            {
                ProductId = productId,
                ProductVariantId = productVariantId,
                Page = page,
                PageSize = pageSize
            };

            var result = await _queryDispatcher.Send<GetAdminProductImagesQuery, PagedResult<AdminProductImageDto>>(query);
            return Ok(result);
        }

        /// <summary>Uploads a new image for a product (or variant)</summary>
        [HttpPost]
        public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductImageCommand command)
        {
            if (command.ProductId != productId)
                return BadRequest("Product ID mismatch");

            var result = await _commandDispatcher.Send<CreateProductImageCommand, AdminProductImageDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a product image</summary>
        [HttpDelete("{imageId:guid}")]
        public async Task<IActionResult> Delete(Guid productId, Guid imageId)
        {
            var command = new DeleteProductImageCommand { Id = imageId };
            await _commandDispatcher.Send<DeleteProductImageCommand, Unit>(command);
            return NoContent();
        }
    }

    [ApiController]
    [Route("api/admin/attributes")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminProductAttributeController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminProductAttributeController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all product attributes</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isVariant = null)
        {
            var query = new GetAdminProductAttributesQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                IsVariant = isVariant
            };

            var result = await _queryDispatcher.Send<GetAdminProductAttributesQuery, PagedResult<AdminProductAttributeDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific attribute by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminProductAttributeByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminProductAttributeByIdQuery, AdminProductAttributeDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new product attribute</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductAttributeCommand command)
        {
            var attribute = await _commandDispatcher.Send<CreateProductAttributeCommand, AdminProductAttributeDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = attribute.Id }, attribute);
        }

        /// <summary>Updates an existing product attribute</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductAttributeCommand command)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var attribute = await _commandDispatcher.Send<UpdateProductAttributeCommand, AdminProductAttributeDto>(command);
            return Ok(attribute);
        }

        /// <summary>Deletes a product attribute</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteProductAttributeCommand { Id = id };
            await _commandDispatcher.Send<DeleteProductAttributeCommand, Unit>(command);
            return NoContent();
        }
    }
}