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
    [Route("api/admin/vendors")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminVendorController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminVendorController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all vendors (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminVendorsQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                IsActive = isActive
            };

            var result = await _queryDispatcher.Send<GetAdminVendorsQuery, PagedResult<VendorDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific vendor by ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminVendorByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminVendorByIdQuery, VendorDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new vendor</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVendorCommand command)
        {
            var result = await _commandDispatcher.Send<CreateVendorCommand, VendorDto>(command);
            return Ok(result);
        }

        /// <summary>Updates an existing vendor</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorCommand command)
        {
            command.Id = id;
            var result = await _commandDispatcher.Send<UpdateVendorCommand, VendorDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a vendor</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteVendorCommand { Id = id };
            await _commandDispatcher.Send<DeleteVendorCommand, Unit>(command);
            return NoContent();
        }

        /// <summary>Gets all products for a vendor</summary>
        [HttpGet("{id:guid}/products")]
        public async Task<IActionResult> GetProducts(Guid id)
        {
            var query = new GetVendorProductsQuery { VendorId = id };
            var result = await _queryDispatcher.Send<GetVendorProductsQuery, List<VendorProductDto>>(query);
            return Ok(result);
        }

        /// <summary>Adds a product to a vendor</summary>
        [HttpPost("{id:guid}/products")]
        public async Task<IActionResult> AddProduct(Guid id, [FromBody] CreateVendorProductCommand command)
        {
            command.VendorId = id;
            var result = await _commandDispatcher.Send<CreateVendorProductCommand, VendorProductDto>(command);
            return Ok(result);
        }

        /// <summary>Removes a product from a vendor</summary>
        [HttpDelete("products/{id:guid}")]
        public async Task<IActionResult> RemoveProduct(Guid id)
        {
            var command = new DeleteVendorProductCommand { Id = id };
            await _commandDispatcher.Send<DeleteVendorProductCommand, Unit>(command);
            return NoContent();
        }
    }
}