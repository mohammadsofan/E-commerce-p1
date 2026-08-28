using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Inventory;
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
            var wasFulfilled = order.FulfillmentStatus == FulfillmentStatus.Delivered;

            // Blocks cancellation from terminal states (cancelled/completed/refunded).
            order.Cancel(command.Reason ?? string.Empty);

            // 1. Release reserved inventory. A delivered order has already had its
            //    reservation consumed into on-hand stock, so there is nothing to release.
            if (!wasFulfilled && order.Items != null && order.Items.Any())
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
                    var variantId = item.ProductVariantId == Guid.Empty ? (Guid?)null : item.ProductVariantId;
                    var candidates = InventoryAllocator.CandidatesFor(inventoryItems, item.ProductId, variantId);
                    InventoryAllocator.Release(candidates, item.Quantity);
                }
            }

            // 1b. Give the coupon back: a cancelled order must not consume a single-use
            //     or per-user-limited coupon.
            await ReleaseCouponUsageAsync(order, cancellationToken);

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

        /// <summary>
        /// Returns a coupon to the customer when their order is cancelled: the usage row is
        /// removed and the aggregate counter decremented, so a single-use or per-user-limited
        /// coupon is not consumed by an order that never completed.
        /// </summary>
        private async Task ReleaseCouponUsageAsync(Order order, CancellationToken cancellationToken)
        {
            var usages = await _db.CouponUsages
                .Where(u => u.OrderId == order.Id)
                .ToListAsync(cancellationToken);

            if (usages.Count == 0) return;

            var couponIds = usages.Select(u => u.CouponId).Distinct().ToList();
            var coupons = await _db.Coupons
                .Where(c => couponIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            foreach (var coupon in coupons)
            {
                var released = usages.Count(u => u.CouponId == coupon.Id);
                coupon.UsedCount = Math.Max(0, coupon.UsedCount - released);
            }

            _db.CouponUsages.RemoveRange(usages);
        }
    }
}
