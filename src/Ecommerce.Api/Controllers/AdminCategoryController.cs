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
    [Route("api/admin/categories")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminCategoryController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminCategoryController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all categories (admin view)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _queryDispatcher.Send<GetCategoriesQuery, List<CategoryDto>>(new GetCategoriesQuery());
            return Ok(result);
        }

        /// <summary>Gets a category by id</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _queryDispatcher.Send<GetCategoryByIdQuery, CategoryDto>(
                new GetCategoryByIdQuery { Id = id });
            return Ok(result);
        }

        /// <summary>Gets a category by slug</summary>
        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var result = await _queryDispatcher.Send<GetCategoryBySlugQuery, CategoryDto>(
                new GetCategoryBySlugQuery { Slug = slug });
            return Ok(result);
        }

        /// <summary>Creates a new category</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            var result = await _commandDispatcher.Send<CreateCategoryCommand, CategoryDto>(command);
            return Ok(result);
        }

        /// <summary>Updates an existing category</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryCommand command)
        {
            command.Id = id;
            var result = await _commandDispatcher.Send<UpdateCategoryCommand, CategoryDto>(command);
            return Ok(result);
        }

        /// <summary>Deletes a category</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteCategoryCommand { Id = id };
            await _commandDispatcher.Send<DeleteCategoryCommand, Unit>(command);
            return NoContent();
        }
    }
}