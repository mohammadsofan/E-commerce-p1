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
            [FromQuery] string? sortBy = null,
            [FromQuery] string? tag = null)
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
                SortBy = sortBy,
                Tag = tag
            };
            var result = await _queryDispatcher.Send<GetProductsQuery, Ecommerce.Application.Common.PagedResult<ProductDto>>(query);
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

        [HttpPost("recommendations")]
        public async Task<IActionResult> GetRecommendations([FromBody] GetFrequentlyBoughtTogetherQuery query)
        {
            var result = await _queryDispatcher.Send<GetFrequentlyBoughtTogetherQuery, List<ProductDto>>(query ?? new GetFrequentlyBoughtTogetherQuery());
            return Ok(result);
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendationsGet([FromQuery] List<Guid>? productIds, [FromQuery] int limit = 4)
        {
            var query = new GetFrequentlyBoughtTogetherQuery(productIds ?? new List<Guid>(), limit);
            var result = await _queryDispatcher.Send<GetFrequentlyBoughtTogetherQuery, List<ProductDto>>(query);
            return Ok(result);
        }
    }
}
