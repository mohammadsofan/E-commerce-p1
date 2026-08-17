using System.IO;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/stripe/webhook")]
    [AllowAnonymous]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IStripeWebhookService _webhookService;

        public StripeWebhookController(IStripeWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string jsonBody;
            using (var reader = new StreamReader(Request.Body))
            {
                jsonBody = await reader.ReadToEndAsync();
            }

            var signature = Request.Headers["Stripe-Signature"].ToString();
            var result = await _webhookService.HandleWebhookAsync(jsonBody, signature, HttpContext.RequestAborted);

            if (!result.SignatureValid)
                return Unauthorized(new { error = result.Message });

            return Ok(new { handled = result.Handled, eventType = result.EventType, message = result.Message });
        }
    }
}