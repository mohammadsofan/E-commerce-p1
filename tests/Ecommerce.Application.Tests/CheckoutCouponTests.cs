using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class CheckoutCouponTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private CheckoutCommandHandler CreateHandler(ApplicationDbContext ctx, IPromotionEvaluationService? promoEvaluator = null)
        {
            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(ctx);
            return new CheckoutCommandHandler(ctx, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher(), null, promoEvaluator);
        }

        private async Task<(Guid productId, Guid variantId)> SeedInventory(ApplicationDbContext ctx, int stock)
        {
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var inv = new InventoryItem { Id = variantId, ProductId = productId, ProductVariantId = variantId };
            inv.AddStock(stock);
            await ctx.InventoryItems.AddAsync(inv);
            await ctx.SaveChangesAsync();
            return (productId, variantId);
        }

        [Fact]
        public async Task Checkout_WithPercentageCoupon_AppliesDiscount()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "SAVE20",
                Type = "percentage",
                Value = 20,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var handler = CreateHandler(ctx);
            var orderId = await handler.Handle(new CheckoutCommand { ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                CouponCode = "save20",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 }
                }
            });

            var order = await ctx.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId);
            // 2 items x 10 = 20 subtotal; 20% = 4 discount; +15 shipping = 31 total
            Assert.Equal(20m, order.Subtotal);
            Assert.Equal(4m, order.DiscountAmount);
            Assert.Equal(15m, order.ShippingAmount);
            Assert.Equal(31m, order.TotalAmount);
            Assert.Equal("SAVE20", order.CouponCode);
        }

        [Fact]
        public async Task Checkout_WithFixedAmountCoupon_AppliesDiscount()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "FLAT5",
                Type = "fixed_amount",
                Value = 5,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var handler = CreateHandler(ctx);
            var orderId = await handler.Handle(new CheckoutCommand { ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                CouponCode = "FLAT5",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 1 }
                }
            });

            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal(10m, order.Subtotal);
            Assert.Equal(5m, order.DiscountAmount);
            Assert.Equal(15m, order.ShippingAmount);
            Assert.Equal(20m, order.TotalAmount);
        }

        [Fact]
        public async Task Checkout_WithMaxDiscountCap_ClampsDiscount()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "CAPPED",
                Type = "percentage",
                Value = 50,
                MaxDiscountAmount = 3m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var handler = CreateHandler(ctx);
            var orderId = await handler.Handle(new CheckoutCommand { ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                CouponCode = "CAPPED",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 }
                }
            });

            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            // 20 subtotal, 50% = 10 but capped at 3; +15 shipping = 32 total
            Assert.Equal(3m, order.DiscountAmount);
            Assert.Equal(15m, order.ShippingAmount);
            Assert.Equal(32m, order.TotalAmount);
        }

        [Fact]
        public async Task Checkout_WithInvalidCoupon_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);

            var handler = CreateHandler(ctx);
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new CheckoutCommand { ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                CouponCode = "NOPE",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 1 }
                }
            }));
        }

        [Fact]
        public async Task Checkout_WithExpiredCoupon_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "EXPIRED",
                Type = "percentage",
                Value = 20,
                IsActive = true,
                EndAt = DateTimeOffset.UtcNow.AddDays(-1),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var handler = CreateHandler(ctx);
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new CheckoutCommand { ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                CouponCode = "EXPIRED",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 1 }
                }
            }));
        }

        [Fact]
        public async Task Checkout_WithoutCoupon_NoDiscount()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);

            var handler = CreateHandler(ctx);
            var orderId = await handler.Handle(new CheckoutCommand { ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 1 }
                }
            });

            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal(0m, order.DiscountAmount);
            Assert.Equal(string.Empty, order.CouponCode);
        }

        [Fact]
        public async Task Checkout_WithCoupon_RecordsClampedDiscountInCouponUsage()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);
            var userId = Guid.NewGuid();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "OVER50",
                Type = "fixed_amount",
                Value = 50m, // 50 discount on 20 subtotal
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var handler = CreateHandler(ctx);
            var orderId = await handler.Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = userId,
                Currency = "USD",
                CouponCode = "OVER50",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 } // 20 subtotal
                }
            });

            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            var usage = await ctx.CouponUsages.FirstAsync(u => u.OrderId == orderId);

            Assert.Equal(20m, order.DiscountAmount); // Clamped to subtotal (20)
            Assert.Equal(20m, usage.DiscountAmount); // Usage table must record clamped 20, NOT 50
        }

        [Fact]
        public async Task Checkout_WithMinOrderAmountAndCartLevelPromotion_ValidatesAgainstApplicableSubtotal()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedInventory(ctx, 50);
            var userId = Guid.NewGuid();

            // Promotion: Spend 100 get 30 fixed discount
            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Cart Promo 30",
                Type = "tiered_discount",
                RulesJson = "{\"tiers\": [{\"minSpend\": 100, \"discount\": 30, \"discountType\": \"fixed_amount\"}]}",
                IsActive = true,
                Priority = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Promotions.AddAsync(promo);

            // Coupon: Requires MinOrderAmount 100
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "MIN100",
                Type = "fixed_amount",
                Value = 10m,
                MinOrderAmount = 100m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var promoService = new Ecommerce.Infrastructure.Services.PromotionEvaluationService(ctx);
            var handler = CreateHandler(ctx, promoService);

            // Gross subtotal = 110 (11 items x 10). Cart promo = 30. Applicable subtotal = 80 (< 100 min spend).
            var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = userId,
                Currency = "USD",
                CouponCode = "MIN100",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 11 }
                }
            }));

            Assert.Equal("لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون", ex.Message);
        }
    }
}


