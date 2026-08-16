using System;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Infrastructure.Payments
{
    // Simple stub payment gateway for development/testing.
    public class PaymentGateway : IPaymentService
    {
        public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            // In production, integrate with a real payment provider (Stripe, PayPal, Adyen, etc.)
            var tx = Guid.NewGuid().ToString();
            var result = new PaymentResult
            {
                Success = true,
                TransactionId = tx,
                ErrorMessage = string.Empty
            };

            return Task.FromResult(result);
        }
    }
}
