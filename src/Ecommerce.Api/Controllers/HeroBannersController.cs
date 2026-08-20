using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.HeroBanners;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HeroBannersController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public HeroBannersController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets the current active home page hero banner</summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var query = new GetActiveHeroBannerQuery();
            var result = await _queryDispatcher.Send<GetActiveHeroBannerQuery, HeroBannerDto?>(query);
            return Ok(result);
        }
    }
}
