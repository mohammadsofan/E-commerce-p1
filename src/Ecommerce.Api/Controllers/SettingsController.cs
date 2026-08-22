using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public SettingsController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet("shipping")]
        [AllowAnonymous]
        public async Task<IActionResult> GetShippingSettings()
        {
            var query = new GetShippingSettingsQuery();
            var result = await _queryDispatcher.Send<GetShippingSettingsQuery, ShippingSettingsDto>(query);
            return Ok(result);
        }
    }
}
