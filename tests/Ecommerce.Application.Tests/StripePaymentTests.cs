using System;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class StripePaymentProviderTests
    {
        private static StripePaymentProvider CreateProvider(string secretKey = "sk_test_dummy_key")
        {
            var options = new StripePaymentProvider.StripeOptions
            {
                SecretKey = secretKey,
                PublishableKey = "pk_test_dummy",
                WebhookSecret = "whsec_test",
                TestMode = true
            };
            return new StripePaymentProvider(options, NullLogger<StripePaymentProvider>.Instance);
        }

        [Fact]
        public async Task ProcessPayment_DummyKey_ReturnsSimulatedSuccess()
        {
            var provider = CreateProvider();

            var result = await provider.ProcessPaymentAsync(new PaymentRequest
            {
                Amount = 99.99m,
                Currency = "USD",
                PaymentMethod = "card",
                IdempotencyKey = "idem-1"
            });

            Assert.True(result.Success);
            Assert.StartsWith("pi_", result.TransactionId);
            Assert.Equal("captured", result.Status);
        }

        [Fact]
        public async Task ProcessPayment_WithCaptureDeferred_ReturnsAuthorized()
        {
            var provider = CreateProvider();

            var result = await provider.ProcessPaymentAsync(new PaymentRequest
            {
                Amount = 50m,
                Currency = "USD",
                PaymentMethod = "card",
                IdempotencyKey = "idem-2",
                CaptureImmediately = false
            });

            Assert.True(result.Success);
            Assert.Equal("authorized", result.Status);
        }

        [Fact]
        public async Task ProcessPayment_InvalidAmount_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.ProcessPaymentAsync(new PaymentRequest
            {
                Amount = 0m,
                Currency = "USD",
                PaymentMethod = "card",
                IdempotencyKey = "idem-3"
            });

            Assert.False(result.Success);
            Assert.Contains("positive", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessPayment_UnsupportedMethod_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.ProcessPaymentAsync(new PaymentRequest
            {
                Amount = 10m,
                Currency = "USD",
                PaymentMethod = "crypto",
                IdempotencyKey = "idem-4"
            });

            Assert.False(result.Success);
            Assert.Contains("Unsupported payment method", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessPayment_MissingIdempotencyKey_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.ProcessPaymentAsync(new PaymentRequest
            {
                Amount = 10m,
                Currency = "USD",
                PaymentMethod = "card"
            });

            Assert.False(result.Success);
            Assert.Contains("Idempotency key", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessPayment_ExceedsSimulationLimit_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.ProcessPaymentAsync(new PaymentRequest
            {
                Amount = 20000m,
                Currency = "USD",
                PaymentMethod = "card",
                IdempotencyKey = "idem-5"
            });

            Assert.False(result.Success);
            Assert.Contains("limit", result.ErrorMessage);
        }

        [Fact]
        public async Task CapturePayment_DummyKey_ReturnsSimulatedSuccess()
        {
            var provider = CreateProvider();

            var result = await provider.CapturePaymentAsync("pi_abc123");

            Assert.True(result.Success);
            Assert.Equal("pi_abc123", result.TransactionId);
            Assert.Equal("captured", result.Status);
        }

        [Fact]
        public async Task CapturePayment_MissingId_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.CapturePaymentAsync("");

            Assert.False(result.Success);
            Assert.Contains("Provider payment ID", result.ErrorMessage);
        }

        [Fact]
        public async Task VoidPayment_DummyKey_ReturnsSimulatedSuccess()
        {
            var provider = CreateProvider();

            var result = await provider.VoidPaymentAsync("pi_abc123");

            Assert.True(result.Success);
            Assert.Equal("voided", result.Status);
        }

        [Fact]
        public async Task VoidPayment_MissingId_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.VoidPaymentAsync(null);

            Assert.False(result.Success);
            Assert.Contains("Provider payment ID", result.ErrorMessage);
        }

        [Fact]
        public async Task RefundPayment_DummyKey_ReturnsSimulatedSuccess()
        {
            var provider = CreateProvider();

            var result = await provider.RefundPaymentAsync(new RefundRequest
            {
                ProviderPaymentId = "pi_abc123",
                Amount = 25m,
                Currency = "USD",
                Reason = "Customer request",
                IdempotencyKey = "idem-refund-1"
            });

            Assert.True(result.Success);
            Assert.StartsWith("re_", result.RefundId);
            Assert.Equal("succeeded", result.Status);
        }

        [Fact]
        public async Task RefundPayment_MissingId_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.RefundPaymentAsync(new RefundRequest
            {
                ProviderPaymentId = "",
                Amount = 25m,
                Currency = "USD",
                IdempotencyKey = "idem-refund-2"
            });

            Assert.False(result.Success);
            Assert.Contains("Provider payment ID", result.ErrorMessage);
        }

        [Fact]
        public async Task RefundPayment_InvalidAmount_ReturnsError()
        {
            var provider = CreateProvider();

            var result = await provider.RefundPaymentAsync(new RefundRequest
            {
                ProviderPaymentId = "pi_abc123",
                Amount = -5m,
                Currency = "USD",
                IdempotencyKey = "idem-refund-3"
            });

            Assert.False(result.Success);
            Assert.Contains("positive", result.ErrorMessage);
        }

        [Fact]
        public async Task RealMode_WithPlaceholderKey_StaysSimulated()
        {
            // Even a real-looking test key that starts with sk_test_dummy is treated as test mode.
            var provider = CreateProvider("sk_test_dummy_whatever");

            var result = await provider.ProcessPaymentAsync(new PaymentRequest
            {
                Amount = 10m,
                Currency = "USD",
                PaymentMethod = "card",
                IdempotencyKey = "idem-6"
            });

            Assert.True(result.Success);
        }
    }

    public class StripeWebhookServiceTests
    {
        private static StripeWebhookService CreateService(
            Ecommerce.Infrastructure.Persistence.ApplicationDbContext ctx,
            string webhookSecret = "whsec_testsecret")
        {
            var options = new StripePaymentProvider.StripeOptions
            {
                SecretKey = "sk_test_dummy",
                PublishableKey = "pk_test_dummy",
                WebhookSecret = webhookSecret,
                TestMode = true
            };
            return new StripeWebhookService(ctx, NullLogger<StripeWebhookService>.Instance, Options.Create(options));
        }

        private static Ecommerce.Infrastructure.Persistence.ApplicationDbContext CreateContext()
        {
            var dbOptions = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Ecommerce.Infrastructure.Persistence.ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new Ecommerce.Infrastructure.Persistence.ApplicationDbContext(dbOptions);
        }

        private static string ComputeSignatureHeader(string payload, string secret)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sig = Stripe.EventUtility.ComputeSignature(secret, timestamp.ToString(), payload);
            return $"t={timestamp},v1={sig}";
        }

        [Fact]
        public async Task HandleWebhook_MissingSignature_ReturnsInvalid()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var result = await service.HandleWebhookAsync("{}", "");

            Assert.False(result.SignatureValid);
        }

        [Fact]
        public async Task HandleWebhook_BadSignature_ReturnsInvalid()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var result = await service.HandleWebhookAsync("{}", "t=1,v1=invalid");

            Assert.False(result.SignatureValid);
        }

        [Fact]
        public async Task HandleWebhook_ValidSignature_SucceedsReconcilesPayment()
        {
            using var ctx = CreateContext();

            var payment = new Ecommerce.Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_success123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "authorized",
                PaymentMethod = "card",
                AuthorizedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);

            var payload = "{" +
                "\"id\":\"evt_1\",\"object\":\"event\",\"type\":\"payment_intent.succeeded\"," +
                "\"api_version\":\"2023-10-16\",\"created\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ",\"livemode\":false,\"data\":{" +
                "\"object\":{\"id\":\"pi_success123\",\"object\":\"payment_intent\",\"amount\":10000,\"currency\":\"usd\",\"status\":\"succeeded\"}" +
                "}}";

            var header = ComputeSignatureHeader(payload, "whsec_testsecret");
            var result = await service.HandleWebhookAsync(payload, header);

            Assert.True(result.SignatureValid);
            Assert.True(result.Handled);
            Assert.Equal("payment_intent.succeeded", result.EventType);

            var updated = await ctx.Payments.FindAsync(payment.Id);
            Assert.Equal("captured", updated.Status);
            Assert.NotNull(updated.CapturedAt);
        }

        [Fact]
        public async Task HandleWebhook_PaymentIntentFailed_MarksPaymentFailed()
        {
            using var ctx = CreateContext();

            var payment = new Ecommerce.Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_fail123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "authorized",
                PaymentMethod = "card",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);

            var payload = "{" +
                "\"id\":\"evt_2\",\"object\":\"event\",\"type\":\"payment_intent.payment_failed\"," +
                "\"api_version\":\"2023-10-16\",\"created\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ",\"livemode\":false,\"data\":{" +
                "\"object\":{\"id\":\"pi_fail123\",\"object\":\"payment_intent\",\"amount\":10000,\"currency\":\"usd\",\"status\":\"requires_payment_method\",\"last_payment_error\":{\"message\":\"Your card was declined.\"}}" +
                "}}";

            var header = ComputeSignatureHeader(payload, "whsec_testsecret");
            var result = await service.HandleWebhookAsync(payload, header);

            Assert.True(result.SignatureValid);

            var updated = await ctx.Payments.FindAsync(payment.Id);
            Assert.Equal("failed", updated.Status);
            Assert.NotNull(updated.FailedAt);
            Assert.Contains("declined", updated.FailureReason);
        }

        [Fact]
        public async Task HandleWebhook_PaymentIntentCanceled_MarksVoided()
        {
            using var ctx = CreateContext();

            var payment = new Ecommerce.Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_void123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "authorized",
                PaymentMethod = "card",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);

            var payload = "{" +
                "\"id\":\"evt_3\",\"object\":\"event\",\"type\":\"payment_intent.canceled\"," +
                "\"api_version\":\"2023-10-16\",\"created\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ",\"livemode\":false,\"data\":{" +
                "\"object\":{\"id\":\"pi_void123\",\"object\":\"payment_intent\",\"amount\":10000,\"currency\":\"usd\",\"status\":\"canceled\"}" +
                "}}";

            var header = ComputeSignatureHeader(payload, "whsec_testsecret");
            var result = await service.HandleWebhookAsync(payload, header);

            Assert.True(result.SignatureValid);

            var updated = await ctx.Payments.FindAsync(payment.Id);
            Assert.Equal("voided", updated.Status);
            Assert.NotNull(updated.VoidedAt);
        }

        [Fact]
        public async Task HandleWebhook_ChargeRefunded_RecordsRefund()
        {
            using var ctx = CreateContext();

            var payment = new Ecommerce.Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_refund123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "captured",
                PaymentMethod = "card",
                CapturedAt = DateTimeOffset.UtcNow,
                CapturedAmount = 100m,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);

            var payload = "{" +
                "\"id\":\"evt_4\",\"object\":\"event\",\"type\":\"charge.refunded\"," +
                "\"api_version\":\"2023-10-16\",\"created\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ",\"livemode\":false,\"data\":{" +
                "\"object\":{\"id\":\"ch_1\",\"object\":\"charge\",\"amount\":10000,\"currency\":\"usd\",\"amount_refunded\":5000,\"refunded\":true," +
                "\"payment_intent\":{\"id\":\"pi_refund123\"}," +
                "\"refunds\":{\"object\":\"list\",\"data\":[{\"id\":\"re_1\",\"object\":\"refund\",\"amount\":5000,\"status\":\"succeeded\"}]}" +
                "}}}";

            var header = ComputeSignatureHeader(payload, "whsec_testsecret");
            var result = await service.HandleWebhookAsync(payload, header);

            Assert.True(result.SignatureValid);
            Assert.True(result.Handled);

            var updated = await ctx.Payments.FindAsync(payment.Id);
            Assert.Equal("partially_refunded", updated.Status);
            Assert.Equal(50m, updated.RefundedAmount);
        }

        [Fact]
        public async Task HandleWebhook_UnknownPaymentIntent_StillValidButHandled()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var payload = "{" +
                "\"id\":\"evt_5\",\"object\":\"event\",\"type\":\"payment_intent.succeeded\"," +
                "\"api_version\":\"2023-10-16\",\"created\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ",\"livemode\":false,\"data\":{" +
                "\"object\":{\"id\":\"pi_unknown\",\"object\":\"payment_intent\",\"amount\":10000,\"currency\":\"usd\",\"status\":\"succeeded\"}" +
                "}}";

            var header = ComputeSignatureHeader(payload, "whsec_testsecret");
            var result = await service.HandleWebhookAsync(payload, header);

            Assert.True(result.SignatureValid);
            Assert.True(result.Handled);
        }
    }
}