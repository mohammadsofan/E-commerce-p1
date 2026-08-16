using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;

namespace Ecommerce.Application.Commands.Admin
{
    public class CapturePaymentCommand : ICommand<PaymentResultDto>
    {
        public Guid PaymentId { get; set; }
        public decimal? Amount { get; set; }
    }

    public class VoidPaymentCommand : ICommand<PaymentResultDto>
    {
        public Guid PaymentId { get; set; }
    }

    public class RefundPaymentCommand : ICommand<RefundResultDto>
    {
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class RefundResultDto
    {
        public bool Success { get; set; }
        public string RefundId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}