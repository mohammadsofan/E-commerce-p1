using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/brands")]
    public class BrandsController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public BrandsController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? categoryId = null)
        {
            var query = new GetBrandsQuery { CategoryId = categoryId };
            var result = await _queryDispatcher.Send<GetBrandsQuery, List<BrandDto>>(query);
            return Ok(result);
        }
    }
}
