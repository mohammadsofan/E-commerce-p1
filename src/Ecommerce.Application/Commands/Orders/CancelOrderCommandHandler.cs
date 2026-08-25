using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Orders
{
    public class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, OrderDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly IPaymentService? _paymentService;

        public CancelOrderCommandHandler(
            IApplicationDbContext db,
            IMapper mapper,
            ICurrentUserService currentUser,
            IPaymentService? paymentService = null)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
            _paymentService = paymentService;
        }

        public async Task<OrderDto> Handle(CancelOrderCommand command, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;
            if (!isAdmin && (!userId.HasValue || order.UserId != userId.Value))
                throw new NotFoundException("Order", command.OrderId);

            var wasPaid = order.PaymentStatus == PaymentStatus.Paid;

            // Blocks cancellation from terminal states (cancelled/completed/refunded).
            order.Cancel(command.Reason ?? string.Empty);

            // 1. Release reserved inventory
            if (order.Items != null && order.Items.Any())
            {
                var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
                var variantIds = order.Items
                    .Where(i => i.ProductVariantId != Guid.Empty)
                    .Select(i => i.ProductVariantId)
                    .Distinct()
                    .ToList();

                var inventoryItems = await _db.InventoryItems
                    .Where(inv => productIds.Contains(inv.ProductId) ||
                                  (inv.ProductVariantId.HasValue && variantIds.Contains(inv.ProductVariantId.Value)))
                    .ToListAsync(cancellationToken);

                foreach (var item in order.Items)
                {
                    var matchingInventory = inventoryItems
                        .Where(inv =>
                            (item.ProductVariantId != Guid.Empty && inv.ProductVariantId == item.ProductVariantId)
                            || (item.ProductVariantId == Guid.Empty && inv.ProductId == item.ProductId && !inv.ProductVariantId.HasValue))
                        .OrderByDescending(inv => inv.QuantityReserved)
                        .ToList();

                    int remainingToRelease = item.Quantity;
                    foreach (var inv in matchingInventory)
                    {
                        if (remainingToRelease <= 0) break;
                        if (inv.QuantityReserved <= 0) continue;

                        int canRelease = Math.Min(remainingToRelease, inv.QuantityReserved);
                        if (canRelease > 0)
                        {
                            inv.Release(canRelease);
                            remainingToRelease -= canRelease;
                        }
                    }
                }
            }

            // 2. Automatically refund or void if order had payments
            if (wasPaid && _paymentService != null)
            {
                var payments = await _db.Payments
                    .Include(p => p.Refunds)
                    .Where(p => p.OrderId == order.Id)
                    .ToListAsync(cancellationToken);

                foreach (var payment in payments)
                {
                    if (payment.Status == "captured" || payment.Status == "partially_refunded")
                    {
                        var refundableAmount = payment.Amount - payment.RefundedAmount;
                        if (refundableAmount > 0 && !string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
                        {
                            var refundRequest = new RefundRequest
                            {
                                ProviderPaymentId = payment.ProviderPaymentId,
                                Amount = refundableAmount,
                                Currency = payment.CurrencyCode,
                                Reason = string.IsNullOrWhiteSpace(command.Reason) ? "Order cancelled" : command.Reason,
                                IdempotencyKey = $"refund-cancel-{order.Id}-{payment.Id}"
                            };

                            var refundResult = await _paymentService.RefundPaymentAsync(refundRequest);
                            if (refundResult.Success)
                            {
                                var refund = new Refund
                                {
                                    Id = Guid.NewGuid(),
                                    PaymentId = payment.Id,
                                    ProviderRefundId = !string.IsNullOrWhiteSpace(refundResult.RefundId) ? refundResult.RefundId : $"re_{Guid.NewGuid():N}",
                                    Amount = refundableAmount,
                                    CurrencyCode = payment.CurrencyCode,
                                    Reason = string.IsNullOrWhiteSpace(command.Reason) ? "Order cancelled" : command.Reason,
                                    Status = "succeeded",
                                    ProcessedAt = DateTimeOffset.UtcNow,
                                    CreatedAt = DateTimeOffset.UtcNow,
                                    UpdatedAt = DateTimeOffset.UtcNow
                                };
                                _db.Refunds.Add(refund);

                                payment.RefundedAmount += refundableAmount;
                                payment.Status = "refunded";
                                payment.RefundedAt = DateTimeOffset.UtcNow;
                                payment.UpdatedAt = DateTimeOffset.UtcNow;
                            }
                        }
                    }
                    else if (payment.Status == "authorized" && !string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
                    {
                        var voidResult = await _paymentService.VoidPaymentAsync(payment.ProviderPaymentId);
                        if (voidResult.Success)
                        {
                            payment.Status = "voided";
                            payment.VoidedAt = DateTimeOffset.UtcNow;
                            payment.UpdatedAt = DateTimeOffset.UtcNow;
                        }
                    }
                }

                order.RefundedAmount = order.TotalAmount;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<OrderDto>(order);
        }
    }
}
