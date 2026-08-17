using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Ecommerce.Infrastructure.Payments
{
    /// <summary>
    /// Stripe payment provider backed by the official Stripe.net SDK.
    /// In test mode (dummy/empty keys) it falls back to a local simulation so
    /// the application remains fully functional without real Stripe credentials.
    /// </summary>
    public class StripePaymentProvider : IPaymentService
    {
        private readonly ILogger<StripePaymentProvider> _logger;
        private readonly bool _realMode;

        private readonly PaymentIntentService? _paymentIntentService;
        private readonly RefundService? _refundService;

        public StripePaymentProvider(StripeOptions options, ILogger<StripePaymentProvider> logger)
        {
            _logger = logger;

            // Real mode only when a non-placeholder secret key is configured.
            _realMode = !string.IsNullOrWhiteSpace(options.SecretKey)
                        && !options.SecretKey.StartsWith("sk_test_dummy", StringComparison.OrdinalIgnoreCase)
                        && !options.SecretKey.Equals("sk_test", StringComparison.OrdinalIgnoreCase);

            if (_realMode)
            {
                var client = new StripeClient(options.SecretKey);
                _paymentIntentService = new PaymentIntentService(client);
                _refundService = new RefundService(client);
            }
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            if (request == null)
                return new PaymentResult { Success = false, ErrorMessage = "Payment request is null" };

            if (request.Amount <= 0)
                return new PaymentResult { Success = false, ErrorMessage = "Amount must be positive" };

            if (string.IsNullOrWhiteSpace(request.Currency))
                return new PaymentResult { Success = false, ErrorMessage = "Currency is required" };

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return new PaymentResult { Success = false, ErrorMessage = "Idempotency key is required for payment processing" };

            // Validate payment method
            var supportedMethods = new[] { "card", "bank_transfer", "digital_wallet" };
            var method = request.PaymentMethod?.ToLowerInvariant() ?? "card";
            if (!supportedMethods.Contains(method))
                return new PaymentResult { Success = false, ErrorMessage = $"Unsupported payment method: {request.PaymentMethod}" };

            try
            {
                if (!_realMode)
                {
                    _logger.LogWarning("Stripe not configured with a real secret key; simulating payment in test mode.");
                    return SimulateProcess(request);
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = ToMinorUnits(request.Amount, request.Currency),
                    Currency = request.Currency.ToLowerInvariant(),
                    PaymentMethodTypes = new List<string> { MapPaymentMethod(method) },
                    ConfirmationMethod = request.CaptureImmediately ? null : "manual",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["idempotency_key"] = request.IdempotencyKey,
                        ["payment_method"] = method
                    }
                };

                var requestOptions = new RequestOptions { IdempotencyKey = request.IdempotencyKey };
                var paymentIntent = await _paymentIntentService!.CreateAsync(options, requestOptions);

                return new PaymentResult
                {
                    Success = paymentIntent.Status is "succeeded" or "requires_capture" or "processing" or "requires_payment_method" or "requires_confirmation" or "requires_action",
                    TransactionId = paymentIntent.Id,
                    Status = paymentIntent.Status == "requires_capture" ? "authorized" : paymentIntent.Status
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe payment processing failed: {Message}", ex.Message);
                return new PaymentResult { Success = false, ErrorMessage = $"Payment processing failed: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed: {Message}", ex.Message);
                return new PaymentResult { Success = false, ErrorMessage = $"Payment processing failed: {ex.Message}" };
            }
        }

        public async Task<PaymentResult> CapturePaymentAsync(string providerPaymentId, decimal? amount = null)
        {
            if (string.IsNullOrWhiteSpace(providerPaymentId))
                return new PaymentResult { Success = false, ErrorMessage = "Provider payment ID is required" };

            try
            {
                if (!_realMode)
                {
                    _logger.LogWarning("Stripe not configured with a real secret key; simulating capture in test mode.");
                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = providerPaymentId,
                        Status = "captured"
                    };
                }

                var options = new PaymentIntentCaptureOptions
                {
                    AmountToCapture = amount.HasValue ? (long?)ToMinorUnits(amount.Value) : null
                };
                var paymentIntent = await _paymentIntentService!.CaptureAsync(providerPaymentId, options);

                return new PaymentResult
                {
                    Success = paymentIntent.Status == "succeeded",
                    TransactionId = paymentIntent.Id,
                    Status = "captured"
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe capture failed: {Message}", ex.Message);
                return new PaymentResult { Success = false, ErrorMessage = $"Capture failed: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Capture failed: {Message}", ex.Message);
                return new PaymentResult { Success = false, ErrorMessage = $"Capture failed: {ex.Message}" };
            }
        }

        public async Task<PaymentResult> VoidPaymentAsync(string providerPaymentId)
        {
            if (string.IsNullOrWhiteSpace(providerPaymentId))
                return new PaymentResult { Success = false, ErrorMessage = "Provider payment ID is required" };

            try
            {
                if (!_realMode)
                {
                    _logger.LogWarning("Stripe not configured with a real secret key; simulating void in test mode.");
                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = providerPaymentId,
                        Status = "voided"
                    };
                }

                var paymentIntent = await _paymentIntentService!.CancelAsync(providerPaymentId);

                return new PaymentResult
                {
                    Success = paymentIntent.Status == "canceled",
                    TransactionId = paymentIntent.Id,
                    Status = "voided"
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe void failed: {Message}", ex.Message);
                return new PaymentResult { Success = false, ErrorMessage = $"Void failed: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Void failed: {Message}", ex.Message);
                return new PaymentResult { Success = false, ErrorMessage = $"Void failed: {ex.Message}" };
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(RefundRequest request)
        {
            if (request == null)
                return new RefundResult { Success = false, ErrorMessage = "Refund request is null" };

            if (string.IsNullOrWhiteSpace(request.ProviderPaymentId))
                return new RefundResult { Success = false, ErrorMessage = "Provider payment ID is required" };

            if (request.Amount <= 0)
                return new RefundResult { Success = false, ErrorMessage = "Refund amount must be positive" };

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return new RefundResult { Success = false, ErrorMessage = "Idempotency key is required for refund processing" };

            try
            {
                if (!_realMode)
                {
                    _logger.LogWarning("Stripe not configured with a real secret key; simulating refund in test mode.");
                    return new RefundResult
                    {
                        Success = true,
                        RefundId = $"re_{Guid.NewGuid().ToString("N")[..24]}",
                        Status = "succeeded"
                    };
                }

                var options = new RefundCreateOptions
                {
                    PaymentIntent = request.ProviderPaymentId,
                    Amount = (long)ToMinorUnits(request.Amount, request.Currency),
                    Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : MapRefundReason(request.Reason)
                };

                var requestOptions = new RequestOptions { IdempotencyKey = request.IdempotencyKey };
                var refund = await _refundService!.CreateAsync(options, requestOptions);

                return new RefundResult
                {
                    Success = refund.Status == "succeeded",
                    RefundId = refund.Id,
                    Status = refund.Status
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe refund failed: {Message}", ex.Message);
                return new RefundResult { Success = false, ErrorMessage = $"Refund failed: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed: {Message}", ex.Message);
                return new RefundResult { Success = false, ErrorMessage = $"Refund failed: {ex.Message}" };
            }
        }

        private static PaymentResult SimulateProcess(PaymentRequest request)
        {
            if (request.Amount > 10000)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Amount exceeds limit for test mode" };
            }

            return new PaymentResult
            {
                Success = true,
                TransactionId = $"pi_{Guid.NewGuid().ToString("N")[..24]}",
                Status = request.CaptureImmediately ? "captured" : "authorized"
            };
        }

        private static long ToMinorUnits(decimal amount, string currency = "usd")
        {
            // Zero-decimal currencies (JPY, VND, KRW, etc.) are not divided.
            var zeroDecimal = new[] { "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf" };
            if (zeroDecimal.Contains(currency.ToLowerInvariant()))
                return (long)Math.Round(amount);

            return (long)Math.Round(amount * 100m);
        }

        private static string MapPaymentMethod(string method) => method.ToLowerInvariant() switch
        {
            "bank_transfer" => "us_bank_account",
            "digital_wallet" => "card",
            _ => "card"
        };

        private static string MapRefundReason(string reason) => reason.ToLowerInvariant() switch
        {
            "requested_by_customer" => "requested_by_customer",
            "duplicate" => "duplicate",
            "fraudulent" => "fraudulent",
            _ => "requested_by_customer"
        };

        /// <summary>
        /// Configuration options for the Stripe payment provider.
        /// </summary>
        public class StripeOptions
        {
            public string SecretKey { get; set; } = string.Empty;
            public string PublishableKey { get; set; } = string.Empty;
            public string WebhookSecret { get; set; } = string.Empty;
            public string ApiVersion { get; set; } = "2023-10-16";
            public bool TestMode { get; set; } = true;
        }
    }
}
