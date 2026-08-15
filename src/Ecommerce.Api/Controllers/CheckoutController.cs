using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Application.Common.Commands;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly CommandDispatcher _dispatcher;

        public CheckoutController(CommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CheckoutCommand command)
        {
            var orderId = await _dispatcher.Send<CheckoutCommand, System.Guid>(command);
            return Accepted(new { orderId });
        }
    }
}
