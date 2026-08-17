using System;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Infrastructure.Payments
{
    /// <summary>
    /// Legacy payment gateway stub - kept for backward compatibility.
    /// Use StripePaymentProvider for new implementations.
    /// </summary>
    [Obsolete("Use StripePaymentProvider instead. This will be removed in a future version.")]
    public class PaymentGateway : IPaymentService
    {
        public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            var tx = Guid.NewGuid().ToString();
            var result = new PaymentResult
            {
                Success = true,
                TransactionId = tx,
                ErrorMessage = string.Empty,
                Status = "captured"
            };

            return Task.FromResult(result);
        }

        public Task<PaymentResult> CapturePaymentAsync(string providerPaymentId, decimal? amount = null)
        {
            return Task.FromResult(new PaymentResult { Success = true, TransactionId = providerPaymentId, Status = "captured" });
        }

        public Task<PaymentResult> VoidPaymentAsync(string providerPaymentId)
        {
            return Task.FromResult(new PaymentResult { Success = true, TransactionId = providerPaymentId, Status = "voided" });
        }

        public Task<RefundResult> RefundPaymentAsync(RefundRequest request)
        {
            return Task.FromResult(new RefundResult { Success = true, RefundId = Guid.NewGuid().ToString(), Status = "succeeded" });
        }
    }
}
