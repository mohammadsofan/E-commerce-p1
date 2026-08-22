using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Application.Common.Commands;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CheckoutController : ControllerBase
    {
        private readonly CommandDispatcher _dispatcher;

        public CheckoutController(CommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("CouponRateLimit")]
        public async Task<IActionResult> Post([FromBody] CheckoutCommand command)
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub) && System.Guid.TryParse(sub, out var userId))
            {
                command.UserId = userId;
            }

            var orderId = await _dispatcher.Send<CheckoutCommand, System.Guid>(command);
            return Ok(new { orderId });
        }
    }
}
