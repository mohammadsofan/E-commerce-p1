using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Products;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public ProductsController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = new GetProductsQuery { Page = page, PageSize = pageSize };
            var result = await _queryDispatcher.Send<GetProductsQuery, List<ProductDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetProductByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetProductByIdQuery, ProductDto>(query);
            return Ok(result);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();
            var query = new GetProductBySlugQuery { Slug = slug };
            var result = await _queryDispatcher.Send<GetProductBySlugQuery, ProductDto>(query);
            return Ok(result);
        }
    }
}
