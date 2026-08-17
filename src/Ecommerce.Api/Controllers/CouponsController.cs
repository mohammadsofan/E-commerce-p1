using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CouponsController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public CouponsController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateCouponRequest request)
        {
            var query = new ValidateCouponQuery
            {
                Code = request.Code,
                UserId = request.UserId,
                OrderTotal = request.OrderTotal,
                ProductIds = request.ProductIds,
                CategoryIds = request.CategoryIds
            };
            var result = await _queryDispatcher.Send<ValidateCouponQuery, ValidateCouponResponse>(query);
            return Ok(result);
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] CalculateDiscountsQuery query)
        {
            var result = await _queryDispatcher.Send<CalculateDiscountsQuery, DiscountCalculationResult>(query);
            return Ok(result);
        }
    }
}