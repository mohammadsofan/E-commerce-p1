using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IStripeWebhookService
    {
        /// <summary>
        /// Validates the Stripe webhook signature and processes the event,
        /// updating the local payment/refund state accordingly.
        /// </summary>
        /// <param name="jsonBody">Raw request body.</param>
        /// <param name="signatureHeader">Value of the Stripe-Signature header.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<StripeWebhookResult> HandleWebhookAsync(string jsonBody, string signatureHeader, System.Threading.CancellationToken cancellationToken = default);
    }

    public class StripeWebhookResult
    {
        public bool SignatureValid { get; set; }
        public bool Handled { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}