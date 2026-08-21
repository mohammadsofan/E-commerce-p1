using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Shipping;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class ShippingController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public ShippingController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetMethods()
        {
            var result = await _queryDispatcher.Send<GetActiveShippingMethodsQuery, List<ShippingMethodDto>>(new GetActiveShippingMethodsQuery());
            return Ok(result);
        }

        [HttpGet("zones")]
        public async Task<IActionResult> GetZones()
        {
            var result = await _queryDispatcher.Send<GetActiveShippingZonesQuery, List<ShippingZoneDto>>(new GetActiveShippingZonesQuery());
            return Ok(result);
        }
    }
}
