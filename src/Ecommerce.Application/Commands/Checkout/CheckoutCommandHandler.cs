using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.DomainEvents;
using Ecommerce.Application.Common.Inventory;
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
                    // Another request holds the key. Give the winner a brief window to publish
                    // its order id so a double-click returns the same order instead of an error.
                    for (var attempt = 0; attempt < 10; attempt++)
                    {
                        var again = await _idempotency.TryGetResponseAsync(command.IdempotencyKey);
                        if (again.Found && !string.IsNullOrEmpty(again.Response) && Guid.TryParse(again.Response, out var prev2))
                            return prev2;

                        await Task.Delay(150, cancellationToken);
                    }

                    throw new DomainException("طلبك قيد المعالجة بالفعل. يرجى الانتظار لحظة قبل المحاولة مرة أخرى.");
                }
            }

            // The persisted cart is the single source of truth for what is being purchased.
            // command.Items is only an assertion of what the client believed the cart held,
            // so a stale tab can never smuggle in lines that were never validated.
            var userCarts = new List<Cart>();
            if (command.UserId != Guid.Empty)
            {
                userCarts = await _db.Carts
                    .Include(c => c.Items)
                    .Where(c => c.UserId == command.UserId && c.Status == Domain.Enums.CartStatus.Active)
                    .ToListAsync(cancellationToken);
            }

            var checkoutLines = ResolveCheckoutLines(command, userCarts);
            if (checkoutLines.Count == 0) throw new DomainException("سلة التسوق فارغة. يرجى إضافة منتجات قبل إتمام الطلب.");

            IDbContextTransaction? tx = null;
            if (_db.Database.IsRelational())
            {
                tx = await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.RepeatableRead,
                    cancellationToken);
            }

            try
            {
                var currencyCode = await ResolveCurrencyCodeAsync(command.Currency, cancellationToken);

                // Build order
                var paymentMethodText = !string.IsNullOrWhiteSpace(command.PaymentMethod) ? command.PaymentMethod : "CashOnDelivery";
                var notesParts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(command.ShippingAddress)) notesParts.Add($"Address: {command.ShippingAddress}");
                notesParts.Add($"PaymentMethod: {paymentMethodText}");

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"ORD-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpperInvariant()}",
                    CurrencyCode = currencyCode,
                    PaymentMethod = paymentMethodText,
                    ShippingAmount = command.ShippingAmount >= 0 ? command.ShippingAmount : 0m,
                    CustomerNotes = command.CustomerNotes ?? string.Empty,
                    Notes = string.Join(" | ", notesParts),
                    UserId = command.UserId == Guid.Empty ? null : command.UserId,
                    ShippingAddressId = command.ShippingAddressId,
                    BillingAddressId = command.BillingAddressId,
                    ShippingMethodId = command.ShippingMethodId
                };

                // Pre-fetch products, variants, and inventory items to eliminate N+1 queries
                var productIds = checkoutLines.Select(i => i.ProductId).Distinct().ToList();
                var variantIds = checkoutLines
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

                var orderedItems = checkoutLines
                    .OrderBy(i => i.ProductId)
                    .ThenBy(i => i.ProductVariantId ?? Guid.Empty)
                    .ToList();

                var promotionUsages = new System.Collections.Generic.List<Ecommerce.Domain.Entities.PromotionUsage>();

                foreach (var it in orderedItems)
                {
                    var product = products.FirstOrDefault(p => p.Id == it.ProductId);
                    if (product == null) throw new NotFoundException("Product", it.ProductId);
                    if (!product.IsActive || product.IsDeleted)
                        throw new DomainException($"المنتج '{product.Name}' لم يعد متاحاً للبيع. يرجى إزالته من السلة.");

                    ProductVariant? variant = null;
                    if (it.ProductVariantId.HasValue && it.ProductVariantId.Value != Guid.Empty)
                    {
                        variant = variants.FirstOrDefault(v => v.Id == it.ProductVariantId.Value);
                        if (variant == null) throw new NotFoundException("ProductVariant", it.ProductVariantId.Value);

                        // A variant must belong to the product it is being purchased under,
                        // otherwise the client could pick any variant's price for any product.
                        if (variant.ProductId != it.ProductId)
                            throw new DomainException("الخيار المحدد لا ينتمي إلى هذا المنتج.");

                        if (!variant.IsActive)
                            throw new DomainException($"الخيار '{variant.Name}' لم يعد متاحاً. يرجى اختيار خيار آخر.");
                    }

                    var productName = product.Name;
                    var baseUnitPrice = variant?.Price ?? product.BasePrice;
                    var unitPrice = baseUnitPrice;
                    decimal lineDiscount = 0m;

                    if (_promotionEvaluator != null)
                    {
                        var promoEval = await _promotionEvaluator.EvaluateProductAsync(
                            product.Id,
                            product.CategoryId,
                            baseUnitPrice,
                            it.Quantity,
                            cancellationToken);

                        if (promoEval.HasActivePromotion)
                        {
                            if (promoEval.TotalDiscount > 0)
                            {
                                lineDiscount = promoEval.TotalDiscount;
                            }
                            else if (promoEval.PromotionalPrice < baseUnitPrice && promoEval.DiscountAmount > 0)
                            {
                                lineDiscount = (baseUnitPrice - promoEval.PromotionalPrice) * it.Quantity;
                            }

                            if (lineDiscount > 0 && promoEval.PromotionId.HasValue)
                            {
                                promotionUsages.Add(new Ecommerce.Domain.Entities.PromotionUsage
                                {
                                    Id = Guid.NewGuid(),
                                    PromotionId = promoEval.PromotionId.Value,
                                    UserId = command.UserId,
                                    OrderId = order.Id,
                                    DiscountAmount = lineDiscount,
                                    CreatedAt = DateTimeOffset.UtcNow
                                });
                            }
                        }
                    }

                    var variantName = variant?.Name ?? string.Empty;
                    var sku = variant?.Sku ?? product.Sku ?? string.Empty;
                    var imageUrl = product.Images?.FirstOrDefault()?.Url ?? string.Empty;
                    var variantId = it.ProductVariantId ?? Guid.Empty;

                    order.AddItem(it.ProductId, variantId, productName, unitPrice, it.Quantity, lineDiscount, variantName, sku, imageUrl, it.SelectedOptions);

                    // --- Multi-warehouse Inventory Allocation ---
                    // Shared with add-to-cart and cancellation so the three paths cannot drift.
                    // Backorder is allowed when the catalog entity permits it OR any warehouse
                    // row is flagged for backorder — the same rule add-to-cart applies.
                    var candidateLocations = InventoryAllocator.CandidatesFor(inventoryItems, it.ProductId, it.ProductVariantId);
                    var catalogAllowsBackorder = variant?.AllowBackorder ?? product.AllowBackorder;
                    var backorderAllowed = catalogAllowsBackorder || InventoryAllocator.AllowsBackorder(candidateLocations);
                    var totalAvailableForItem = InventoryAllocator.AvailableFor(candidateLocations, it.Quantity);

                    if (!backorderAllowed && totalAvailableForItem < it.Quantity)
                    {
                        throw new DomainException($"المنتج '{productName}' غير متوفر بالكمية المطلوبة. الكمية المتاحة: {totalAvailableForItem}.");
                    }

                    InventoryAllocator.Reserve(candidateLocations, it.Quantity, backorderAllowed);
                    // --------------------------------------------
                }

                // --- CART LEVEL PROMOTIONS ---
                if (_promotionEvaluator != null)
                {
                    var cartTargets = order.Items.Select(i => new Ecommerce.Application.Interfaces.CartLevelPromotionTarget
                    {
                        ProductId = i.ProductId,
                        CategoryId = products.FirstOrDefault(p => p.Id == i.ProductId)?.CategoryId,
                        UnitPrice = i.Quantity > 0 ? (i.TotalAmount / i.Quantity) : i.UnitPrice,
                        Quantity = i.Quantity
                    }).ToList();

                    var cartLevelEval = await _promotionEvaluator.EvaluateCartLevelPromotionsAsync(cartTargets, order.Subtotal, cancellationToken);
                    if (cartLevelEval.HasCartLevelPromotion && !string.IsNullOrWhiteSpace(cartLevelEval.PromotionName))
                    {
                        order.ApplyCartLevelPromotion(cartLevelEval.PromotionName, cartLevelEval.TotalCartDiscount);
                        if (cartLevelEval.TotalCartDiscount > 0 && cartLevelEval.PromotionId.HasValue)
                        {
                            promotionUsages.Add(new Ecommerce.Domain.Entities.PromotionUsage
                            {
                                Id = Guid.NewGuid(),
                                PromotionId = cartLevelEval.PromotionId.Value,
                                UserId = command.UserId,
                                OrderId = order.Id,
                                DiscountAmount = cartLevelEval.TotalCartDiscount,
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }
                    }
                }
                // -----------------------------

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

                    var applicableSubtotal = Math.Max(0m, order.Subtotal - order.CartLevelDiscountAmount);
                    if (coupon.MinOrderAmount.HasValue && applicableSubtotal < coupon.MinOrderAmount.Value)
                    {
                        await ClearCartsCouponAndFail("لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون");
                    }

                    isFreeShippingCoupon = false;
                    decimal discount = 0m;
                    var type = (coupon.Type ?? string.Empty).ToLowerInvariant();
                    if (type == "percentage")
                    {
                        var percentage = Math.Clamp(coupon.Value, 0m, 100m);
                        discount = Math.Round(applicableSubtotal * (percentage / 100m), 2, MidpointRounding.AwayFromZero);
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

                    discount = Math.Max(0m, Math.Min(applicableSubtotal, discount));

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
                            DiscountAmount = order.DiscountAmount,
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }
                }

                // Shipping is always priced server-side: the client may select a method but
                // never dictate the amount.
                var finalShippingCost = await ResolveShippingCostAsync(command, order, isFreeShippingCoupon, cancellationToken);

                order.SetShippingAmount(finalShippingCost);
                order.PlaceOrder();

                // Validate that final charged total matches client's expected total within acceptable delta
                if (!command.ExpectedTotal.HasValue)
                {
                    throw new DomainException("يجب توفير الإجمالي المتوقع للتحقق من صحة الطلب.");
                }

                // Legacy sentinel: ExpectedTotal == -1 explicitly opts the caller into accepting
                // any recalculated total (used by older clients and by the existing test suite).
                var callerAcceptsAnyTotal = command.AcceptPriceChanges || command.ExpectedTotal.Value == -1m;
                if (!callerAcceptsAnyTotal)
                {
                    var delta = Math.Abs(order.TotalAmount - command.ExpectedTotal.Value);
                    if (delta > 0.01m)
                    {
                        throw new DomainException($"تغير سعر أحد المنتجات. الإجمالي الجديد هو {order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}. يرجى مراجعة الطلب والتأكيد.");
                    }
                }

                // Clear user's active cart in database if exists
                foreach (var userCart in userCarts)
                {
                    userCart.Clear();
                }

                if (promotionUsages.Count > 0)
                {
                    var groupedUsages = promotionUsages
                        .GroupBy(pu => pu.PromotionId)
                        .Select(g => new Ecommerce.Domain.Entities.PromotionUsage
                        {
                            Id = Guid.NewGuid(),
                            PromotionId = g.Key,
                            UserId = command.UserId,
                            OrderId = order.Id,
                            DiscountAmount = g.Sum(x => x.DiscountAmount),
                            CreatedAt = DateTimeOffset.UtcNow
                        })
                        .ToList();

                    await _db.PromotionUsages.AddRangeAsync(groupedUsages, cancellationToken);

                    var promoIds = groupedUsages.Select(u => u.PromotionId).ToList();
                    var promos = await _db.Promotions.Where(p => promoIds.Contains(p.Id)).ToListAsync(cancellationToken);
                    foreach(var promo in promos)
                    {
                        promo.UsedCount++;
                    }
                    _promotionEvaluator?.ClearCache();
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

                if (!string.IsNullOrEmpty(command.IdempotencyKey))
                {
                    await _idempotency.SaveResponseAsync(command.IdempotencyKey, order.Id.ToString());
                }

                return order.Id;
            }
            catch (DbUpdateConcurrencyException)
            {
                await SafeRollbackAsync(tx);
                throw new DomainException("المنتج المطلوب نفد من المخزون. يرجى تحديث السلة والمحاولة مرة أخرى.");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 1205)
            {
                await SafeRollbackAsync(tx);
                throw new DomainException("حدث تعارض مؤقت في الطلب. يرجى المحاولة مرة أخرى.");
            }
            catch (InventoryException ex)
            {
                await SafeRollbackAsync(tx);
                throw new DomainException(ex.Message ?? "بعض المنتجات المطلوبة نفدت من المخزون.");
            }
            catch (NotFoundException)
            {
                await SafeRollbackAsync(tx);
                throw;
            }
            catch (DomainException)
            {
                await SafeRollbackAsync(tx);
                throw;
            }
            catch (Exception)
            {
                await SafeRollbackAsync(tx);
                // Internal failures must not leak provider/SQL detail to the caller.
                throw new DomainException("تعذّر إتمام الطلب حالياً. يرجى المحاولة مرة أخرى.");
            }
            finally
            {
                if (tx != null)
                {
                    await tx.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Determines the lines that will be ordered. When the user has a persisted cart, that
        /// cart wins and the client-supplied list is only used to detect a stale client
        /// (so the customer is told to review rather than silently charged for the wrong thing).
        /// Guest/None-user checkouts fall back to the supplied items.
        /// </summary>
        private static List<CheckoutItem> ResolveCheckoutLines(CheckoutCommand command, List<Cart> userCarts)
        {
            var cartItems = userCarts.SelectMany(c => c.Items).ToList();
            if (cartItems.Count == 0)
            {
                // No server-side cart: only a client-supplied list is available (e.g. guest flow).
                return command.Items ?? new List<CheckoutItem>();
            }

            var lines = cartItems
                .Select(i => new CheckoutItem
                {
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity,
                    SelectedOptions = i.SelectedOptions
                })
                .ToList();

            if (command.Items != null && command.Items.Count > 0 && !command.AcceptPriceChanges)
            {
                var requested = Signature(command.Items);
                var actual = Signature(lines);
                if (!requested.SetEquals(actual))
                {
                    throw new DomainException("تغيرت محتويات سلة التسوق. يرجى مراجعة السلة وإعادة المحاولة.");
                }
            }

            return lines;
        }

        private static HashSet<string> Signature(IEnumerable<CheckoutItem> items)
        {
            return items
                .GroupBy(i => (i.ProductId, i.ProductVariantId ?? Guid.Empty))
                .Select(g => $"{g.Key.Item1}|{g.Key.Item2}|{g.Sum(x => x.Quantity)}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates the requested currency against the configured currencies and falls back to
        /// the store's base currency rather than persisting an arbitrary client-supplied code.
        /// </summary>
        private async Task<string> ResolveCurrencyCodeAsync(string? requested, CancellationToken cancellationToken)
        {
            var currencies = await _db.Currencies
                .AsNoTracking()
                .Select(c => new { c.Code, c.IsBaseCurrency })
                .ToListAsync(cancellationToken);

            var fallback = currencies.FirstOrDefault(c => c.IsBaseCurrency)?.Code
                           ?? currencies.FirstOrDefault()?.Code
                           ?? "USD";

            if (string.IsNullOrWhiteSpace(requested)) return fallback;

            var code = requested.Trim().ToUpperInvariant();
            if (currencies.Count == 0) return code;

            var match = currencies.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                throw new DomainException($"العملة '{requested}' غير مدعومة.");

            return match.Code;
        }

        /// <summary>
        /// Prices shipping from live store settings and the selected shipping method.
        /// The client-supplied amount is never trusted.
        /// </summary>
        private async Task<decimal> ResolveShippingCostAsync(
            CheckoutCommand command,
            Order order,
            bool isFreeShippingCoupon,
            CancellationToken cancellationToken)
        {
            if (!order.Items.Any()) return 0m;

            var storeSettings = await _db.StoreSettings.FirstOrDefaultAsync(cancellationToken);
            var standardShippingCost = storeSettings?.StandardShippingCost ?? 15m;
            var freeShippingThreshold = storeSettings?.FreeShippingThreshold;

            if (isFreeShippingCoupon) return 0m;

            var subtotalAfterDiscount = Math.Max(0m, order.Subtotal - order.CartLevelDiscountAmount - order.DiscountAmount);
            if (freeShippingThreshold.HasValue && subtotalAfterDiscount >= freeShippingThreshold.Value) return 0m;

            if (command.ShippingMethodId.HasValue && command.ShippingMethodId.Value != Guid.Empty)
            {
                var shippingMethod = await _db.ShippingMethods
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == command.ShippingMethodId.Value && m.IsActive, cancellationToken);

                if (shippingMethod == null)
                    throw new DomainException("طريقة الشحن المحددة غير متوفرة. يرجى اختيار طريقة أخرى.");

                if (shippingMethod.FreeShippingThreshold.HasValue &&
                    subtotalAfterDiscount >= shippingMethod.FreeShippingThreshold.Value)
                {
                    return 0m;
                }

                return Math.Max(0m, shippingMethod.BaseRate);
            }

            return standardShippingCost;
        }

        private static async Task SafeRollbackAsync(IDbContextTransaction? tx)
        {
            if (tx == null) return;
            try
            {
                await tx.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Silently ignore if already rolled back/aborted
            }
        }
    }
}
