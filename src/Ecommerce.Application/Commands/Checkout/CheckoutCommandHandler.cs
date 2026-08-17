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
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0,6)}",
                CurrencyCode = command.Currency,
                ShippingAmount = 0m,
                UserId = command.UserId
            };

            foreach (var it in command.Items)
            {
                // In a full implementation, lookup product details
                order.AddItem(it.ProductId, it.ProductVariantId, "Product", 10m, it.Quantity);

                // Reserve inventory
                var inventory = await _db.InventoryItems.FindAsync(new object[] { it.ProductVariantId }, cancellationToken);
                if (inventory == null)
                {
                    // Try by product id
                    inventory = await _db.InventoryItems.FindAsync(new object[] { it.ProductId }, cancellationToken);
                }

                if (inventory == null)
                {
                    throw new InventoryException("Inventory item not found for product/variant");
                }

                inventory.Reserve(it.Quantity);
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

            // Persist order
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
