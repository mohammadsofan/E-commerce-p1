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
    [Route("api/admin/brands")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminBrandController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminBrandController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all brands (admin view)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _queryDispatcher.Send<GetBrandsQuery, List<BrandDto>>(new GetBrandsQuery());
            return Ok(result);
        }

        /// <summary>Creates a new brand</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBrandCommand command)
        {
            var result = await _commandDispatcher.Send<CreateBrandCommand, BrandDto>(command);
            return Ok(result);
        }

        /// <summary>Updates an existing brand</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandCommand command)
        {
            if (id != command.Id)
                return BadRequest("Brand ID mismatch");

            var result = await _commandDispatcher.Send<UpdateBrandCommand, BrandDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a brand</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteBrandCommand { Id = id };
            await _commandDispatcher.Send<DeleteBrandCommand, Unit>(command);
            return NoContent();
        }
    }
}