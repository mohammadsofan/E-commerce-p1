using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Ecommerce.Infrastructure.Payments
{
    /// <summary>
    /// Validates Stripe webhook signatures and reconciles local payment/refund
    /// state based on the events received from Stripe.
    /// </summary>
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly IApplicationDbContext _db;
        private readonly ILogger<StripeWebhookService> _logger;
        private readonly string _webhookSecret;

        public StripeWebhookService(
            IApplicationDbContext db,
            ILogger<StripeWebhookService> logger,
            IOptions<StripePaymentProvider.StripeOptions> options)
        {
            _db = db;
            _logger = logger;
            _webhookSecret = options?.Value?.WebhookSecret ?? string.Empty;
        }

        public async Task<StripeWebhookResult> HandleWebhookAsync(string jsonBody, string signatureHeader, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jsonBody))
                return new StripeWebhookResult { SignatureValid = false, Handled = false, Message = "Empty request body" };

            if (string.IsNullOrWhiteSpace(signatureHeader))
                return new StripeWebhookResult { SignatureValid = false, Handled = false, Message = "Missing Stripe-Signature header" };

            if (string.IsNullOrWhiteSpace(_webhookSecret))
                return new StripeWebhookResult { SignatureValid = false, Handled = false, Message = "Stripe webhook secret is not configured" };

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(jsonBody, signatureHeader, _webhookSecret, throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature validation failed: {Message}", ex.Message);
                return new StripeWebhookResult { SignatureValid = false, Handled = false, Message = "Invalid signature" };
            }

            try
            {
                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                    case "payment_intent.amount_capturable_updated":
                        await HandlePaymentIntentSucceededAsync(stripeEvent, cancellationToken);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentFailedAsync(stripeEvent, cancellationToken);
                        break;

                    case "payment_intent.canceled":
                        await HandlePaymentIntentCanceledAsync(stripeEvent, cancellationToken);
                        break;

                    case "charge.refunded":
                        await HandleChargeRefundedAsync(stripeEvent, cancellationToken);
                        break;

                    default:
                        _logger.LogInformation("Ignoring unhandled Stripe webhook event type: {Type}", stripeEvent.Type);
                        return new StripeWebhookResult { SignatureValid = true, Handled = false, EventType = stripeEvent.Type, Message = "Unhandled event type" };
                }

                return new StripeWebhookResult
                {
                    SignatureValid = true,
                    Handled = true,
                    EventType = stripeEvent.Type,
                    Message = "Processed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Stripe webhook event {Type}", stripeEvent.Type);
                return new StripeWebhookResult { SignatureValid = true, Handled = false, EventType = stripeEvent.Type, Message = ex.Message };
            }
        }

        private async Task HandlePaymentIntentSucceededAsync(Event stripeEvent, CancellationToken cancellationToken)
        {
            if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
            {
                _logger.LogWarning("Webhook event {Type} did not carry a PaymentIntent", stripeEvent.Type);
                return;
            }

            var payment = await FindPaymentAsync(paymentIntent.Id, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment intent {Id} not found locally; skipping reconciliation", paymentIntent.Id);
                return;
            }

            if (paymentIntent.Status == "succeeded")
            {
                payment.Status = "captured";
                payment.CapturedAt = DateTimeOffset.UtcNow;
                payment.CapturedAmount = payment.Amount;

                if (payment.OrderId != Guid.Empty)
                {
                    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);
                    if (order != null && order.Status == Domain.Enums.OrderStatus.Placed)
                    {
                        order.MarkPaid();
                        _logger.LogInformation("Order {OrderId} marked as paid via Stripe webhook", order.Id);
                    }
                }
            }
            else if (paymentIntent.Status == "requires_capture")
            {
                payment.Status = "authorized";
                payment.AuthorizedAt = DateTimeOffset.UtcNow;
            }

            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Payment {Id} reconciled to {Status}", payment.Id, payment.Status);
        }

        private async Task HandlePaymentIntentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
        {
            if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
            {
                _logger.LogWarning("Webhook event {Type} did not carry a PaymentIntent", stripeEvent.Type);
                return;
            }

            var payment = await FindPaymentAsync(paymentIntent.Id, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment intent {Id} not found locally; skipping reconciliation", paymentIntent.Id);
                return;
            }

            payment.Status = "failed";
            payment.FailedAt = DateTimeOffset.UtcNow;
            payment.FailureReason = paymentIntent.LastPaymentError?.Message ?? "Payment failed";
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Payment {Id} marked as failed", payment.Id);
        }

        private async Task HandlePaymentIntentCanceledAsync(Event stripeEvent, CancellationToken cancellationToken)
        {
            if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
            {
                _logger.LogWarning("Webhook event {Type} did not carry a PaymentIntent", stripeEvent.Type);
                return;
            }

            var payment = await FindPaymentAsync(paymentIntent.Id, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment intent {Id} not found locally; skipping reconciliation", paymentIntent.Id);
                return;
            }

            payment.Status = "voided";
            payment.VoidedAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Payment {Id} marked as voided", payment.Id);
        }

        private async Task HandleChargeRefundedAsync(Event stripeEvent, CancellationToken cancellationToken)
        {
            if (stripeEvent.Data.Object is not Charge charge)
            {
                _logger.LogWarning("Webhook event {Type} did not carry a Charge", stripeEvent.Type);
                return;
            }

            var paymentIntentId = charge.PaymentIntent?.Id;
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                _logger.LogWarning("Charge {Id} has no payment intent; skipping reconciliation", charge.Id);
                return;
            }

            var payment = await _db.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.ProviderPaymentId == paymentIntentId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment intent {Id} not found locally; skipping reconciliation", paymentIntentId);
                return;
            }

            var refundedAmount = (decimal)charge.AmountRefunded / 100m;
            var refundAmount = refundedAmount - payment.RefundedAmount;

            if (refundAmount > 0)
            {
                var refund = new Ecommerce.Domain.Entities.Refund
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    ProviderRefundId = charge.Refunds?.Data?.FirstOrDefault()?.Id ?? $"re_{Guid.NewGuid():N}",
                    Amount = refundAmount,
                    CurrencyCode = payment.CurrencyCode,
                    Reason = "Refunded via Stripe",
                    Status = "succeeded",
                    ProcessedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _db.Refunds.Add(refund);
                payment.RefundedAmount += refundAmount;

                if (payment.OrderId != Guid.Empty)
                {
                    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);
                    if (order != null)
                    {
                        order.ProcessRefund(refundAmount, "Refunded via Stripe");
                    }
                }
            }

            payment.Status = payment.RefundedAmount >= payment.Amount ? "refunded" : "partially_refunded";
            if (payment.RefundedAmount >= payment.Amount)
                payment.RefundedAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Payment {Id} refund reconciliation: refunded amount {Amount}", payment.Id, payment.RefundedAmount);
        }

        private Task<Payment?> FindPaymentAsync(string providerPaymentId, CancellationToken cancellationToken)
        {
            return _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId, cancellationToken);
        }
    }
}