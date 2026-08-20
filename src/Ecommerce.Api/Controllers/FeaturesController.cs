using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.StoreFeatures;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeaturesController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public FeaturesController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all active store features for public display (e.g. Home Page)</summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var query = new GetActiveFeaturesQuery();
            var result = await _queryDispatcher.Send<GetActiveFeaturesQuery, List<StoreFeatureDto>>(query);
            return Ok(result);
        }
    }
}
