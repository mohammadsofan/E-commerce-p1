using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminPaymentHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IMapper CreateMapper()
        {
            return new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        [Fact]
        public async Task CapturePayment_AuthorizedPayment_CapturesSuccessfully()
        {
            using var ctx = CreateInMemoryContext();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_test123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "authorized",
                PaymentMethod = "card",
                AuthorizedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var handler = new CapturePaymentCommandHandler(ctx, new TestPaymentService());

            var result = await handler.Handle(new CapturePaymentCommand { PaymentId = payment.Id, Amount = 100m });

            Assert.True(result.Success);
            Assert.Equal("captured", result.Status);

            var updated = await ctx.Payments.FindAsync(payment.Id);
            Assert.Equal("captured", updated.Status);
            Assert.NotNull(updated.CapturedAt);
        }

        [Fact]
        public async Task CapturePayment_NonAuthorizedPayment_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_test123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "captured",
                PaymentMethod = "card",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var handler = new CapturePaymentCommandHandler(ctx, new TestPaymentService());

            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new CapturePaymentCommand { PaymentId = payment.Id }));
        }

        [Fact]
        public async Task VoidPayment_AuthorizedPayment_VoidsSuccessfully()
        {
            using var ctx = CreateInMemoryContext();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_test123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "authorized",
                PaymentMethod = "card",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var handler = new VoidPaymentCommandHandler(ctx, new TestPaymentService());

            var result = await handler.Handle(new VoidPaymentCommand { PaymentId = payment.Id });

            Assert.True(result.Success);
            Assert.Equal("voided", result.Status);

            var updated = await ctx.Payments.FindAsync(payment.Id);
            Assert.Equal("voided", updated.Status);
            Assert.NotNull(updated.VoidedAt);
        }

        [Fact]
        public async Task RefundPayment_CapturedPayment_RefundsSuccessfully()
        {
            using var ctx = CreateInMemoryContext();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_test123",
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

            var idempotencyService = new TestIdempotencyService();
            var handler = new RefundPaymentCommandHandler(ctx, new TestPaymentService(), idempotencyService);

            var result = await handler.Handle(new RefundPaymentCommand
            {
                PaymentId = payment.Id,
                Amount = 50m,
                Reason = "Customer request",
                IdempotencyKey = "idem-123"
            });

            Assert.True(result.Success);
            Assert.Equal("succeeded", result.Status);

            var updated = await ctx.Payments.FindAsync(payment.Id);
            Assert.Equal("partially_refunded", updated.Status);
            Assert.Equal(50m, updated.RefundedAmount);

            var refund = await ctx.Refunds.FirstOrDefaultAsync(r => r.PaymentId == payment.Id);
            Assert.NotNull(refund);
            Assert.Equal(50m, refund.Amount);
        }

        [Fact]
        public async Task RefundPayment_ExceedsAvailable_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_test123",
                Amount = 100m,
                CurrencyCode = "USD",
                Status = "captured",
                PaymentMethod = "card",
                CapturedAt = DateTimeOffset.UtcNow,
                CapturedAmount = 100m,
                RefundedAmount = 80m,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Payments.AddAsync(payment);
            await ctx.SaveChangesAsync();

            var idempotencyService = new TestIdempotencyService();
            var handler = new RefundPaymentCommandHandler(ctx, new TestPaymentService(), idempotencyService);

            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new RefundPaymentCommand
            {
                PaymentId = payment.Id,
                Amount = 50m, // Only 20 available
                Reason = "Test",
                IdempotencyKey = "idem-456"
            }));
        }

        [Fact]
        public async Task RefundPayment_Idempotency_ReturnsSameResult()
        {
            using var ctx = CreateInMemoryContext();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Provider = "stripe",
                ProviderPaymentId = "pi_test123",
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

            var idempotencyService = new TestIdempotencyService();
            var handler = new RefundPaymentCommandHandler(ctx, new TestPaymentService(), idempotencyService);

            var result1 = await handler.Handle(new RefundPaymentCommand
            {
                PaymentId = payment.Id,
                Amount = 25m,
                Reason = "Test",
                IdempotencyKey = "idem-same"
            });

            var result2 = await handler.Handle(new RefundPaymentCommand
            {
                PaymentId = payment.Id,
                Amount = 25m,
                Reason = "Test",
                IdempotencyKey = "idem-same"
            });

            Assert.Equal(result1.RefundId, result2.RefundId);
            Assert.Equal(result1.Status, result2.Status);
        }
    }

    public class TestPaymentService : IPaymentService
    {
        public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            return Task.FromResult(new PaymentResult { Success = true, TransactionId = "pi_test", Status = "authorized" });
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
            return Task.FromResult(new RefundResult { Success = true, RefundId = "re_test123", Status = "succeeded" });
        }
    }

    public class TestIdempotencyService : IIdempotencyService
    {
        private readonly Dictionary<string, string> _store = new();

        public Task<(bool Found, string? Response)> TryGetResponseAsync(string key)
        {
            if (_store.TryGetValue(key, out var response))
                return Task.FromResult((true, response));
            return Task.FromResult((false, (string?)null));
        }

        public Task<bool> TryRegisterAsync(string key, string requestHash, Guid ownerId)
        {
            return Task.FromResult(true);
        }

        public Task SaveResponseAsync(string key, string response)
        {
            _store[key] = response;
            return Task.CompletedTask;
        }
    }
}