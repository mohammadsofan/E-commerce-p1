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

        /// <summary>Gets all active home page hero banners for slider</summary>
        [HttpGet]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveBanners()
        {
            var query = new GetActiveHeroBannersQuery();
            var result = await _queryDispatcher.Send<GetActiveHeroBannersQuery, List<HeroBannerDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets the single latest active home page hero banner (legacy fallback)</summary>
        [HttpGet("active/first")]
        public async Task<IActionResult> GetActiveFirst()
        {
            var query = new GetActiveHeroBannerQuery();
            var result = await _queryDispatcher.Send<GetActiveHeroBannerQuery, HeroBannerDto?>(query);
            return Ok(result);
        }
    }
}
