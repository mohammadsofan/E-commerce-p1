using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentResult> CapturePaymentAsync(string providerPaymentId, decimal? amount = null);
        Task<PaymentResult> VoidPaymentAsync(string providerPaymentId);
        Task<RefundResult> RefundPaymentAsync(RefundRequest request);
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public bool CaptureImmediately { get; set; } = true;
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // authorized, captured
    }

    public class RefundRequest
    {
        public string ProviderPaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string RefundId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, succeeded
    }
}
