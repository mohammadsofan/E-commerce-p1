using System;

namespace Ecommerce.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderPaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public string Status { get; set; } = string.Empty; // authorized, captured, voided, refunded, failed, partially_refunded
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTimeOffset? AuthorizedAt { get; set; }
        public DateTimeOffset? CapturedAt { get; set; }
        public DateTimeOffset? VoidedAt { get; set; }
        public DateTimeOffset? RefundedAt { get; set; }
        public DateTimeOffset? FailedAt { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public decimal RefundedAmount { get; set; }
        public decimal CapturedAmount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation properties
        public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }

    public class Refund
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public string ProviderRefundId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, succeeded, failed
        public DateTimeOffset? ProcessedAt { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Payment Payment { get; set; } = null!;
    }
}
