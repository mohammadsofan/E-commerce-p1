using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public CategoriesController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = new GetCategoriesQuery();
            var result = await _queryDispatcher.Send<GetCategoriesQuery, List<CategoryDto>>(query);
            return Ok(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var query = new GetCategoryBySlugQuery { Slug = slug };
            var result = await _queryDispatcher.Send<GetCategoryBySlugQuery, CategoryDto>(query);
            return Ok(result);
        }
    }
}
