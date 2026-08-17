using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Products;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;
        private readonly IProductSearchService _searchService;

        public ProductsController(QueryDispatcher queryDispatcher, IProductSearchService searchService)
        {
            _queryDispatcher = queryDispatcher;
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] Guid? brandId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? sortBy = null)
        {
            var query = new GetProductsQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                CategoryId = categoryId,
                BrandId = brandId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                IsActive = isActive,
                SortBy = sortBy
            };
            var result = await _queryDispatcher.Send<GetProductsQuery, List<ProductDto>>(query);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("Search term is required");
            var result = await _searchService.SearchAsync(q, page, pageSize);
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
