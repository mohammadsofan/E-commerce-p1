using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CapturePaymentCommandHandler : ICommandHandler<CapturePaymentCommand, PaymentResultDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IPaymentService _paymentService;

        public CapturePaymentCommandHandler(IApplicationDbContext db, IPaymentService paymentService)
        {
            _db = db;
            _paymentService = paymentService;
        }

        public async Task<PaymentResultDto> Handle(CapturePaymentCommand command, CancellationToken cancellationToken = default)
        {
            var payment = await _db.Payments.FindAsync(new object[] { command.PaymentId }, cancellationToken);

            if (payment == null)
                throw new Domain.Exceptions.NotFoundException("Payment", command.PaymentId);

            if (payment.Status != "authorized")
                throw new Domain.Exceptions.DomainException($"Cannot capture payment with status: {payment.Status}");

            var result = await _paymentService.CapturePaymentAsync(payment.ProviderPaymentId, command.Amount);

            if (!result.Success)
                throw new Domain.Exceptions.DomainException($"Capture failed: {result.ErrorMessage}");

            payment.Status = "captured";
            payment.CapturedAt = DateTimeOffset.UtcNow;
            payment.CapturedAmount = command.Amount ?? payment.Amount;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new PaymentResultDto
            {
                Success = true,
                TransactionId = result.TransactionId,
                Status = result.Status
            };
        }
    }

    public class VoidPaymentCommandHandler : ICommandHandler<VoidPaymentCommand, PaymentResultDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IPaymentService _paymentService;

        public VoidPaymentCommandHandler(IApplicationDbContext db, IPaymentService paymentService)
        {
            _db = db;
            _paymentService = paymentService;
        }

        public async Task<PaymentResultDto> Handle(VoidPaymentCommand command, CancellationToken cancellationToken = default)
        {
            var payment = await _db.Payments.FindAsync(new object[] { command.PaymentId }, cancellationToken);

            if (payment == null)
                throw new Domain.Exceptions.NotFoundException("Payment", command.PaymentId);

            if (payment.Status != "authorized")
                throw new Domain.Exceptions.DomainException($"Cannot void payment with status: {payment.Status}");

            var result = await _paymentService.VoidPaymentAsync(payment.ProviderPaymentId);

            if (!result.Success)
                throw new Domain.Exceptions.DomainException($"Void failed: {result.ErrorMessage}");

            payment.Status = "voided";
            payment.VoidedAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new PaymentResultDto
            {
                Success = true,
                TransactionId = result.TransactionId,
                Status = result.Status
            };
        }
    }

    public class RefundPaymentCommandHandler : ICommandHandler<RefundPaymentCommand, RefundResultDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IIdempotencyService _idempotencyService;

        public RefundPaymentCommandHandler(IApplicationDbContext db, IPaymentService paymentService, IIdempotencyService idempotencyService)
        {
            _db = db;
            _paymentService = paymentService;
            _idempotencyService = idempotencyService;
        }

        public async Task<RefundResultDto> Handle(RefundPaymentCommand command, CancellationToken cancellationToken = default)
        {
            // Check idempotency
            var (found, existingResponse) = await _idempotencyService.TryGetResponseAsync(command.IdempotencyKey);
            if (found && existingResponse != null)
            {
                return System.Text.Json.JsonSerializer.Deserialize<RefundResultDto>(existingResponse);
            }

            var payment = await _db.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == command.PaymentId, cancellationToken);

            if (payment == null)
                throw new Domain.Exceptions.NotFoundException("Payment", command.PaymentId);

            if (payment.Status != "captured" && payment.Status != "partially_refunded")
                throw new Domain.Exceptions.DomainException($"Cannot refund payment with status: {payment.Status}");

            var availableToRefund = payment.Amount - payment.RefundedAmount;
            if (command.Amount > availableToRefund)
                throw new Domain.Exceptions.DomainException($"Refund amount exceeds available amount. Available: {availableToRefund}");

            var refundRequest = new Ecommerce.Application.Interfaces.RefundRequest
            {
                ProviderPaymentId = payment.ProviderPaymentId,
                Amount = command.Amount,
                Currency = payment.CurrencyCode,
                Reason = command.Reason,
                IdempotencyKey = command.IdempotencyKey
            };

            var result = await _paymentService.RefundPaymentAsync(refundRequest);

            if (!result.Success)
                throw new Domain.Exceptions.DomainException($"Refund failed: {result.ErrorMessage}");

            // Create refund record
            var refund = new Refund
            {
                PaymentId = payment.Id,
                ProviderRefundId = result.RefundId,
                Amount = command.Amount,
                CurrencyCode = payment.CurrencyCode,
                Reason = command.Reason,
                Status = "succeeded",
                ProcessedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.Refunds.Add(refund);

            // Update payment
            payment.RefundedAmount += command.Amount;
            payment.Status = payment.RefundedAmount >= payment.Amount ? "refunded" : "partially_refunded";
            if (payment.RefundedAmount >= payment.Amount)
                payment.RefundedAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            var response = new RefundResultDto
            {
                Success = true,
                RefundId = result.RefundId,
                Status = result.Status
            };

            // Store idempotency response
            await _idempotencyService.SaveResponseAsync(command.IdempotencyKey, System.Text.Json.JsonSerializer.Serialize(response));

            return response;
        }
    }
}