using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.DomainEvents;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Checkout
{
    public class CheckoutCommandHandler : Ecommerce.Application.Common.Commands.ICommandHandler<CheckoutCommand, System.Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly IIdempotencyService _idempotency;
        private readonly IDomainEventDispatcher _domainEvents;

        public CheckoutCommandHandler(IApplicationDbContext db, IIdempotencyService idempotency, IDomainEventDispatcher domainEvents)
        {
            _db = db;
            _idempotency = idempotency;
            _domainEvents = domainEvents;
        }

        public async Task<System.Guid> Handle(CheckoutCommand command, CancellationToken cancellationToken = default)
        {
            // If idempotency key provided, check for existing response or register
            if (!string.IsNullOrEmpty(command.IdempotencyKey))
            {
                var existing = await _idempotency.TryGetResponseAsync(command.IdempotencyKey);
                if (existing.Found && !string.IsNullOrEmpty(existing.Response))
                {
                    // previous response exists; return the same order id
                    if (Guid.TryParse(existing.Response, out var prev)) return prev;
                }

                // register attempt (simple request hash)
                var requestHash = System.BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(command.UserId + "|" + command.Items.Count));
                var registered = await _idempotency.TryRegisterAsync(command.IdempotencyKey, requestHash, command.UserId);
                if (!registered)
                {
                    // Another request is in progress or already recorded; try to fetch response
                    var again = await _idempotency.TryGetResponseAsync(command.IdempotencyKey);
                    if (again.Found && !string.IsNullOrEmpty(again.Response) && Guid.TryParse(again.Response, out var prev2)) return prev2;
                    throw new DomainException("Unable to register idempotency key; request already in flight");
                }
            }
            if (command.Items == null || !command.Items.Any()) throw new DomainException("No items to checkout");

            // Build order
            var paymentMethodText = !string.IsNullOrWhiteSpace(command.PaymentMethod) ? command.PaymentMethod : "CashOnDelivery";
            var notesParts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(command.ShippingAddress)) notesParts.Add($"Address: {command.ShippingAddress}");
            notesParts.Add($"PaymentMethod: {paymentMethodText}");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpperInvariant()}",
                CurrencyCode = string.IsNullOrWhiteSpace(command.Currency) ? "USD" : command.Currency,
                ShippingAmount = command.ShippingAmount >= 0 ? command.ShippingAmount : 0m,
                CustomerNotes = command.CustomerNotes ?? string.Empty,
                Notes = string.Join(" | ", notesParts),
                UserId = command.UserId == Guid.Empty ? null : command.UserId
            };

            foreach (var it in command.Items)
            {
                var product = await _db.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == it.ProductId, cancellationToken);

                ProductVariant? variant = null;
                if (it.ProductVariantId.HasValue && it.ProductVariantId.Value != Guid.Empty)
                {
                    variant = await _db.ProductVariants
                        .FirstOrDefaultAsync(v => v.Id == it.ProductVariantId.Value, cancellationToken);
                }

                var productName = product?.Name ?? "Product";
                var unitPrice = variant?.Price ?? product?.BasePrice ?? 10m;
                var variantName = variant?.Name ?? string.Empty;
                var sku = variant?.Sku ?? product?.Sku ?? string.Empty;
                var imageUrl = product?.Images?.FirstOrDefault()?.Url ?? string.Empty;
                var variantId = it.ProductVariantId ?? Guid.Empty;

                order.AddItem(it.ProductId, variantId, productName, unitPrice, it.Quantity, 0m, variantName, sku, imageUrl);

                // Reserve inventory if exists
                var inventory = await _db.InventoryItems
                    .FirstOrDefaultAsync(inv => (it.ProductVariantId.HasValue && it.ProductVariantId.Value != Guid.Empty && inv.ProductVariantId == it.ProductVariantId.Value)
                                             || (inv.ProductId == it.ProductId), cancellationToken);

                if (inventory != null)
                {
                    inventory.Reserve(it.Quantity);
                }
            }

            // Apply coupon discount if provided
            if (!string.IsNullOrWhiteSpace(command.CouponCode))
            {
                var coupon = await _db.Coupons
                    .FirstOrDefaultAsync(c => c.Code == command.CouponCode.Trim().ToUpperInvariant(), cancellationToken);
                if (coupon == null)
                    throw new DomainException("Invalid coupon code");

                var now = DateTimeOffset.UtcNow;
                if (!coupon.IsActive)
                    throw new DomainException("Coupon is not active");
                if (coupon.StartAt.HasValue && coupon.StartAt.Value > now)
                    throw new DomainException("Coupon has not started yet");
                if (coupon.EndAt.HasValue && coupon.EndAt.Value < now)
                    throw new DomainException("Coupon has expired");
                if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                    throw new DomainException("Coupon usage limit reached");
                if (coupon.MinOrderAmount.HasValue && order.Subtotal < coupon.MinOrderAmount.Value)
                    throw new DomainException($"Minimum order amount for this coupon is {coupon.MinOrderAmount.Value}");

                decimal discount = 0m;
                if (coupon.Type == "percentage")
                    discount = order.Subtotal * (coupon.Value / 100m);
                else if (coupon.Type == "fixed_amount")
                    discount = coupon.Value;

                if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
                    discount = coupon.MaxDiscountAmount.Value;

                order.ApplyCoupon(coupon.Code, discount);
            }

            order.PlaceOrder();

            // Clear user's active cart in database if exists
            if (command.UserId != Guid.Empty)
            {
                var userCart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == command.UserId && c.Status == Domain.Enums.CartStatus.Active, cancellationToken);

                if (userCart != null)
                {
                    userCart.Clear();
                }
            }

            // Persist order and cleared cart
            await _db.Orders.AddAsync(order, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            // Dispatch any domain events raised during placement (e.g. OrderPlaced).
            var events = order.DomainEvents.ToList();
            order.ClearDomainEvents();
            if (events.Count > 0)
            {
                await _domainEvents.DispatchAsync(events, cancellationToken);
            }

            if (!string.IsNullOrEmpty(command.IdempotencyKey))
            {
                await _idempotency.SaveResponseAsync(command.IdempotencyKey, order.Id.ToString());
            }

            return order.Id;
        }
    }
}
