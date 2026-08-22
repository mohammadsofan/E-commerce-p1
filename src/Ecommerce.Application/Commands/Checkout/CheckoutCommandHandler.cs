using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.DomainEvents;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ecommerce.Application.Commands.Checkout
{
    public class CheckoutCommandHandler : Ecommerce.Application.Common.Commands.ICommandHandler<CheckoutCommand, System.Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly IIdempotencyService _idempotency;
        private readonly IDomainEventDispatcher _domainEvents;
        private readonly IEmailService? _emailService;
        private readonly IPromotionEvaluationService? _promotionEvaluator;

        public CheckoutCommandHandler(
            IApplicationDbContext db,
            IIdempotencyService idempotency,
            IDomainEventDispatcher domainEvents,
            IEmailService? emailService = null,
            IPromotionEvaluationService? promotionEvaluator = null)
        {
            _db = db;
            _idempotency = idempotency;
            _domainEvents = domainEvents;
            _emailService = emailService;
            _promotionEvaluator = promotionEvaluator;
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

            IDbContextTransaction? tx = null;
            if (_db.Database.IsRelational())
            {
                tx = await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.RepeatableRead,
                    cancellationToken);
            }

            try
            {
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

                // Pre-fetch products, variants, and inventory items to eliminate N+1 queries
                var productIds = command.Items.Select(i => i.ProductId).Distinct().ToList();
                var variantIds = command.Items
                    .Where(i => i.ProductVariantId.HasValue && i.ProductVariantId.Value != Guid.Empty)
                    .Select(i => i.ProductVariantId!.Value)
                    .Distinct()
                    .ToList();

                var products = await _db.Products
                    .Include(p => p.Images)
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                var variants = variantIds.Any()
                    ? await _db.ProductVariants
                        .Where(v => variantIds.Contains(v.Id))
                        .ToListAsync(cancellationToken)
                    : new List<ProductVariant>();

                var inventoryItems = await _db.InventoryItems
                    .Where(inv => productIds.Contains(inv.ProductId) ||
                                  (inv.ProductVariantId.HasValue && variantIds.Contains(inv.ProductVariantId.Value)))
                    .ToListAsync(cancellationToken);

                foreach (var it in command.Items)
                {
                    var product = products.FirstOrDefault(p => p.Id == it.ProductId);
                    ProductVariant? variant = null;
                    if (it.ProductVariantId.HasValue && it.ProductVariantId.Value != Guid.Empty)
                    {
                        variant = variants.FirstOrDefault(v => v.Id == it.ProductVariantId.Value);
                    }

                    var productName = product?.Name ?? "Product";
                    var unitPrice = variant?.Price ?? product?.BasePrice ?? 10m;

                    if (_promotionEvaluator != null && product != null)
                    {
                        var promoEval = await _promotionEvaluator.EvaluateProductAsync(
                            product.Id,
                            product.CategoryId,
                            unitPrice,
                            cancellationToken);

                        if (promoEval.HasActivePromotion && promoEval.PromotionalPrice < unitPrice)
                        {
                            unitPrice = promoEval.PromotionalPrice;
                        }
                    }

                    var variantName = variant?.Name ?? string.Empty;
                    var sku = variant?.Sku ?? product?.Sku ?? string.Empty;
                    var imageUrl = product?.Images?.FirstOrDefault()?.Url ?? string.Empty;
                    var variantId = it.ProductVariantId ?? Guid.Empty;

                    order.AddItem(it.ProductId, variantId, productName, unitPrice, it.Quantity, 0m, variantName, sku, imageUrl, it.SelectedOptions);

                    // Reserve inventory if exists
                    var inventory = inventoryItems.FirstOrDefault(inv =>
                        (it.ProductVariantId.HasValue && it.ProductVariantId.Value != Guid.Empty && inv.ProductVariantId == it.ProductVariantId.Value)
                        || (inv.ProductId == it.ProductId));

                    if (inventory != null)
                    {
                        try
                        {
                            inventory.Reserve(it.Quantity);
                        }
                        catch (InventoryException)
                        {
                            if (tx != null) await tx.RollbackAsync(cancellationToken);
                            throw;
                        }
                    }
                }

                // Retrieve active cart(s) for the user if exists
                var userCarts = new System.Collections.Generic.List<Cart>();
                if (command.UserId != Guid.Empty)
                {
                    userCarts = await _db.Carts
                        .Include(c => c.Items)
                        .Where(c => c.UserId == command.UserId && c.Status == Domain.Enums.CartStatus.Active)
                        .ToListAsync(cancellationToken);
                }

                // Check if coupon is provided via command or stored on the active cart
                var effectiveCouponCode = !string.IsNullOrWhiteSpace(command.CouponCode)
                    ? command.CouponCode.Trim()
                    : userCarts.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.AppliedCouponCode))?.AppliedCouponCode;

                // Just-In-Time Coupon Re-validation before order creation
                Coupon? coupon = null;
                bool isFreeShippingCoupon = false;

                if (!string.IsNullOrWhiteSpace(effectiveCouponCode))
                {
                    var upperCode = effectiveCouponCode.ToUpperInvariant();
                    coupon = await _db.Coupons
                        .FirstOrDefaultAsync(c => c.Code == upperCode, cancellationToken);

                    async Task ClearCartsCouponAndFail(string errorMessage)
                    {
                        foreach (var c in userCarts)
                        {
                            c.RemoveCoupon();
                        }
                        if (userCarts.Count > 0)
                        {
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                        if (tx != null) await tx.RollbackAsync(cancellationToken);
                        throw new DomainException(errorMessage);
                    }

                    if (coupon == null)
                    {
                        await ClearCartsCouponAndFail("كود الخصم غير صحيح");
                    }

                    if (!coupon.IsActive)
                    {
                        await ClearCartsCouponAndFail("عذراً، لم يعد هذا الكوبون صالحاً للاستخدام");
                    }

                    var now = DateTimeOffset.UtcNow;
                    if (coupon.StartAt.HasValue && coupon.StartAt.Value > now)
                    {
                        await ClearCartsCouponAndFail("هذا الكوبون لم يبدأ تفعيله بعد");
                    }

                    if (coupon.EndAt.HasValue && coupon.EndAt.Value < now)
                    {
                        await ClearCartsCouponAndFail("انتهت صلاحية الكوبون");
                    }

                    if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                    {
                        await ClearCartsCouponAndFail("تجاوز الكوبون حد الاستخدام المسموح به");
                    }

                    if (coupon.PerUserLimit.HasValue && command.UserId != Guid.Empty)
                    {
                        var userUsageCount = await _db.CouponUsages
                            .CountAsync(u => u.CouponId == coupon.Id && u.UserId == command.UserId, cancellationToken);

                        if (userUsageCount >= coupon.PerUserLimit.Value)
                        {
                            await ClearCartsCouponAndFail("تجاوزت الحد الأقصى المسموح به لاستخدام هذا الكوبون");
                        }
                    }

                    if (coupon.MinOrderAmount.HasValue && order.Subtotal < coupon.MinOrderAmount.Value)
                    {
                        await ClearCartsCouponAndFail("لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون");
                    }

                    isFreeShippingCoupon = false;
                    decimal discount = 0m;
                    var type = (coupon.Type ?? string.Empty).ToLowerInvariant();
                    if (type == "percentage")
                    {
                        discount = order.Subtotal * (coupon.Value / 100m);
                        if (coupon.MaxDiscountAmount.HasValue && coupon.MaxDiscountAmount.Value > 0)
                        {
                            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
                        }
                    }
                    else if (type == "fixed_amount")
                    {
                        discount = coupon.Value;
                    }
                    else if (type == "free_shipping")
                    {
                        isFreeShippingCoupon = true;
                        discount = 0m;
                    }
                    else
                    {
                        discount = coupon.Value;
                    }

                    discount = Math.Max(0m, Math.Min(order.Subtotal, discount));

                    order.ApplyCoupon(coupon.Code, discount);
                    coupon.UsedCount++;

                    if (order.UserId.HasValue && order.UserId.Value != Guid.Empty)
                    {
                        _db.CouponUsages.Add(new CouponUsage
                        {
                            Id = Guid.NewGuid(),
                            CouponId = coupon.Id,
                            UserId = order.UserId.Value,
                            OrderId = order.Id,
                            DiscountAmount = discount,
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }
                }

                // Calculate dynamic shipping cost based on live StoreSettings
                var storeSettings = await _db.StoreSettings.FirstOrDefaultAsync(cancellationToken);
                var standardShippingCost = storeSettings?.StandardShippingCost ?? 15m;
                var freeShippingThreshold = storeSettings?.FreeShippingThreshold;

                var subtotalAfterDiscount = Math.Max(0m, order.Subtotal - order.DiscountAmount);
                decimal finalShippingCost = 0m;

                if (coupon != null && (coupon.Type ?? string.Empty).ToLowerInvariant() == "free_shipping")
                {
                    finalShippingCost = 0m;
                }
                else if (freeShippingThreshold.HasValue && subtotalAfterDiscount >= freeShippingThreshold.Value)
                {
                    finalShippingCost = 0m;
                }
                else if (order.Items.Any())
                {
                    finalShippingCost = standardShippingCost;
                }

                order.SetShippingAmount(finalShippingCost);
                order.PlaceOrder();

                // Clear user's active cart in database if exists
                foreach (var userCart in userCarts)
                {
                    userCart.Clear();
                }

                // Persist order, coupon usage/increment, and cleared cart atomically
                await _db.Orders.AddAsync(order, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                if (tx != null)
                {
                    await tx.CommitAsync(cancellationToken);
                }

                // Dispatch any domain events raised during placement (e.g. OrderPlaced).
                var events = order.DomainEvents.ToList();
                order.ClearDomainEvents();
                if (events.Count > 0)
                {
                    await _domainEvents.DispatchAsync(events, cancellationToken);
                }

                // Trigger customer order confirmation and admin alert emails
                if (_emailService != null)
                {
                    try
                    {
                        string? customerEmail = null;
                        if (order.UserId.HasValue && order.UserId.Value != Guid.Empty)
                        {
                            customerEmail = await _db.Users
                                .Where(u => u.Id == order.UserId.Value)
                                .Select(u => u.Email)
                                .FirstOrDefaultAsync(cancellationToken);
                        }

                        if (!string.IsNullOrWhiteSpace(customerEmail))
                        {
                            await _emailService.SendOrderConfirmationAsync(order, customerEmail, cancellationToken);
                        }

                        await _emailService.SendAdminOrderAlertAsync(order, cancellationToken);
                    }
                    catch
                    {
                        // Notification failure should not fail successful order creation
                    }
                }

                if (!string.IsNullOrEmpty(command.IdempotencyKey))
                {
                    await _idempotency.SaveResponseAsync(command.IdempotencyKey, order.Id.ToString());
                }

                return order.Id;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (tx != null) await tx.RollbackAsync(cancellationToken);
                throw new DomainException("المنتج المطلوب نفد من المخزون. يرجى تحديث السلة والمحاولة مرة أخرى.");
            }
            catch
            {
                if (tx != null) await tx.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (tx != null)
                {
                    await tx.DisposeAsync();
                }
            }
        }
    }
}
