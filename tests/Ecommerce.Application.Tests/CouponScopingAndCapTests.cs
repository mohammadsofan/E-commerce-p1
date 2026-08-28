using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Carts;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Application.Common.Discounts;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Mappings;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Application.Validators;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    /// <summary>
    /// Regression cover for the three coupon/promotion financial-integrity defects:
    /// D-02 (scoping ignored by cart and checkout), D-07 (unvalidated promotion rules) and
    /// D-21 (MaxDiscountAmount ignored for fixed_amount coupons).
    /// </summary>
    public class CouponScopingAndCapTests
    {
        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IMapper CreateMapper()
        {
            return new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId { get; }
            public string UserName => "test-user";
            public bool IsAdmin => false;

            public FakeCurrentUserService(Guid userId) => UserId = userId;
        }

        private static CheckoutCommandHandler CreateCheckoutHandler(ApplicationDbContext ctx)
        {
            return new CheckoutCommandHandler(
                ctx,
                new Ecommerce.Infrastructure.Services.IdempotencyService(ctx),
                new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());
        }

        /// <summary>Seeds a purchasable product (with variant and stock) at 10 each.</summary>
        private static async Task<(Guid productId, Guid variantId)> SeedProduct(
            ApplicationDbContext ctx,
            Guid? categoryId = null,
            decimal price = 10m,
            int stock = 50)
        {
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();

            await ctx.Products.AddAsync(new Product
            {
                Id = productId,
                Name = "Scoped Test Product",
                Slug = $"scoped-{productId}",
                Sku = $"SKU-{productId}",
                BasePrice = price,
                CurrencyCode = "ILS",
                CategoryId = categoryId,
                IsActive = true,
                TrackInventory = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            await ctx.ProductVariants.AddAsync(new ProductVariant
            {
                Id = variantId,
                ProductId = productId,
                Name = "Default",
                Sku = $"SKU-VAR-{variantId}",
                Price = price,
                IsActive = true,
                TrackInventory = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            var inventory = new InventoryItem { Id = variantId, ProductId = productId, ProductVariantId = variantId };
            inventory.AddStock(stock);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            return (productId, variantId);
        }

        private static Coupon NewCoupon(string code, string type, decimal value, Action<Coupon>? configure = null)
        {
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = code,
                Type = type,
                Value = value,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            configure?.Invoke(coupon);
            return coupon;
        }

        // ---------------------------------------------------------------- D-21

        [Fact]
        public async Task Checkout_FixedAmountCouponWithMaxDiscount_RespectsCap()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedProduct(ctx);
            await ctx.Coupons.AddAsync(NewCoupon("CAP5", "fixed_amount", 40m, c => c.MaxDiscountAmount = 5m));
            await ctx.SaveChangesAsync();

            var orderId = await CreateCheckoutHandler(ctx).Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                CouponCode = "CAP5",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 5 } // 50 subtotal
                }
            });

            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal(50m, order.Subtotal);
            Assert.Equal(5m, order.DiscountAmount); // was 40 before the shared calculator
        }

        [Fact]
        public async Task ApplyCouponToCart_FixedAmountCouponWithMaxDiscount_RespectsCap()
        {
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var (productId, _) = await SeedProduct(db, price: 49m);

            var cart = Cart.Create(userId, null);
            cart.AddItem(productId, null, "Scoped Test Product", 49m, 2); // 98
            db.Carts.Add(cart);
            await db.Coupons.AddAsync(NewCoupon("CAP5", "fixed_amount", 40m, c => c.MaxDiscountAmount = 5m));
            await db.SaveChangesAsync();

            var handler = new ApplyCouponToCartCommandHandler(db, new FakeCurrentUserService(userId), CreateMapper());
            var result = await handler.Handle(new ApplyCouponToCartCommand { Code = "CAP5" });

            Assert.Equal(98m, result.Subtotal);
            Assert.Equal(5m, result.DiscountAmount);
            Assert.Equal(93m, result.TotalAmount);
        }

        [Fact]
        public async Task ValidateCoupon_FixedAmountCouponWithMaxDiscount_AgreesWithCartAndCheckout()
        {
            using var ctx = CreateInMemoryContext();
            await ctx.Coupons.AddAsync(NewCoupon("CAP5", "fixed_amount", 40m, c => c.MaxDiscountAmount = 5m));
            await ctx.SaveChangesAsync();

            var handler = new ValidateCouponQueryHandler(ctx, CreateMapper());
            var response = await handler.Handle(new ValidateCouponQuery { Code = "CAP5", OrderTotal = 98m });

            Assert.True(response.IsValid);
            Assert.Equal(5m, response.DiscountAmount);
        }

        // ---------------------------------------------------------------- D-02

        [Fact]
        public async Task Checkout_ProductScopedCouponOnIneligibleProduct_IsRejected()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedProduct(ctx);
            var eligibleProductId = Guid.NewGuid();

            await ctx.Coupons.AddAsync(NewCoupon("SCOPED50", "percentage", 50m,
                c => c.ApplicableProductIds = $"[\"{eligibleProductId}\"]"));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<DomainException>(() => CreateCheckoutHandler(ctx).Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                CouponCode = "SCOPED50",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 }
                }
            }));

            Assert.Equal(CouponDiscountCalculator.IneligibleProductsMessage, ex.Message);
            Assert.Empty(ctx.Orders);
        }

        [Fact]
        public async Task Checkout_ExcludedProductCoupon_IsRejected()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedProduct(ctx);

            await ctx.Coupons.AddAsync(NewCoupon("EXCL50", "percentage", 50m,
                c => c.ExcludedProductIds = $"[\"{productId}\"]"));
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<DomainException>(() => CreateCheckoutHandler(ctx).Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                CouponCode = "EXCL50",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 }
                }
            }));

            Assert.Equal(CouponDiscountCalculator.IneligibleProductsMessage, ex.Message);
        }

        [Fact]
        public async Task Checkout_CategoryScopedCouponOnOutOfCategoryProduct_IsRejected()
        {
            using var ctx = CreateInMemoryContext();
            var productCategoryId = Guid.NewGuid();
            var couponCategoryId = Guid.NewGuid();
            var (productId, variantId) = await SeedProduct(ctx, productCategoryId);

            await ctx.Coupons.AddAsync(NewCoupon("CAT50", "percentage", 50m,
                c => c.ApplicableCategoryIds = $"[\"{couponCategoryId}\"]"));
            await ctx.SaveChangesAsync();

            await Assert.ThrowsAsync<DomainException>(() => CreateCheckoutHandler(ctx).Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                CouponCode = "CAT50",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 }
                }
            }));
        }

        [Fact]
        public async Task Checkout_CategoryScopedCouponOnInCategoryProduct_AppliesDiscount()
        {
            using var ctx = CreateInMemoryContext();
            var categoryId = Guid.NewGuid();
            var (productId, variantId) = await SeedProduct(ctx, categoryId);

            await ctx.Coupons.AddAsync(NewCoupon("CAT50", "percentage", 50m,
                c => c.ApplicableCategoryIds = $"[\"{categoryId}\"]"));
            await ctx.SaveChangesAsync();

            var orderId = await CreateCheckoutHandler(ctx).Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                CouponCode = "CAT50",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 } // 20
                }
            });

            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal(10m, order.DiscountAmount);
        }

        [Fact]
        public async Task Checkout_ScopedCoupon_DiscountsOnlyEligibleLines()
        {
            using var ctx = CreateInMemoryContext();
            var (eligibleId, eligibleVariantId) = await SeedProduct(ctx, price: 100m);
            var (otherId, otherVariantId) = await SeedProduct(ctx, price: 100m);

            await ctx.Coupons.AddAsync(NewCoupon("HALF", "percentage", 50m,
                c => c.ApplicableProductIds = $"[\"{eligibleId}\"]"));
            await ctx.SaveChangesAsync();

            var orderId = await CreateCheckoutHandler(ctx).Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                CouponCode = "HALF",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = eligibleId, ProductVariantId = eligibleVariantId, Quantity = 1 },
                    new CheckoutItem { ProductId = otherId, ProductVariantId = otherVariantId, Quantity = 1 }
                }
            });

            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal(200m, order.Subtotal);
            // 50% of the eligible 100 only — not 50% of the 200 cart.
            Assert.Equal(50m, order.DiscountAmount);
        }

        [Fact]
        public async Task ApplyCouponToCart_ScopedCouponOnIneligibleProduct_IsRejectedAndNotStored()
        {
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var (productId, _) = await SeedProduct(db, price: 49m);
            var eligibleProductId = Guid.NewGuid();

            var cart = Cart.Create(userId, null);
            cart.AddItem(productId, null, "Scoped Test Product", 49m, 2);
            db.Carts.Add(cart);
            await db.Coupons.AddAsync(NewCoupon("SCOPED50", "percentage", 50m,
                c => c.ApplicableProductIds = $"[\"{eligibleProductId}\"]"));
            await db.SaveChangesAsync();

            var handler = new ApplyCouponToCartCommandHandler(db, new FakeCurrentUserService(userId), CreateMapper());

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new ApplyCouponToCartCommand { Code = "SCOPED50" }));

            Assert.Equal(CouponDiscountCalculator.IneligibleProductsMessage, ex.Message);

            var stored = await db.Carts.AsNoTracking().FirstAsync(c => c.Id == cart.Id);
            Assert.True(string.IsNullOrEmpty(stored.AppliedCouponCode));
        }

        [Fact]
        public async Task ApplyCouponToCart_ExcludedProduct_IsRejected()
        {
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var (productId, _) = await SeedProduct(db, price: 49m);

            var cart = Cart.Create(userId, null);
            cart.AddItem(productId, null, "Scoped Test Product", 49m, 2);
            db.Carts.Add(cart);
            await db.Coupons.AddAsync(NewCoupon("EXCL50", "percentage", 50m,
                c => c.ExcludedProductIds = $"[\"{productId}\"]"));
            await db.SaveChangesAsync();

            var handler = new ApplyCouponToCartCommandHandler(db, new FakeCurrentUserService(userId), CreateMapper());

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new ApplyCouponToCartCommand { Code = "EXCL50" }));

            Assert.Equal(CouponDiscountCalculator.IneligibleProductsMessage, ex.Message);
        }

        [Fact]
        public async Task ValidateCoupon_ScopedCouponOnIneligibleProduct_IsInvalid()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, _) = await SeedProduct(ctx);
            var eligibleProductId = Guid.NewGuid();

            await ctx.Coupons.AddAsync(NewCoupon("SCOPED50", "percentage", 50m,
                c => c.ApplicableProductIds = $"[\"{eligibleProductId}\"]"));
            await ctx.SaveChangesAsync();

            var handler = new ValidateCouponQueryHandler(ctx, CreateMapper());
            var response = await handler.Handle(new ValidateCouponQuery
            {
                Code = "SCOPED50",
                OrderTotal = 98m,
                ProductIds = new List<Guid> { productId }
            });

            Assert.False(response.IsValid);
            Assert.Equal(CouponDiscountCalculator.IneligibleProductsMessage, response.ErrorMessage);
        }

        [Fact]
        public async Task ValidateCoupon_CategoryScopedCoupon_ResolvesCategoryFromCatalog()
        {
            // The caller only sends product ids; the handler must still reject a
            // category-scoped coupon whose category the product does not belong to.
            using var ctx = CreateInMemoryContext();
            var (productId, _) = await SeedProduct(ctx, Guid.NewGuid());

            await ctx.Coupons.AddAsync(NewCoupon("CAT50", "percentage", 50m,
                c => c.ApplicableCategoryIds = $"[\"{Guid.NewGuid()}\"]"));
            await ctx.SaveChangesAsync();

            var handler = new ValidateCouponQueryHandler(ctx, CreateMapper());
            var response = await handler.Handle(new ValidateCouponQuery
            {
                Code = "CAT50",
                OrderTotal = 98m,
                ProductIds = new List<Guid> { productId }
            });

            Assert.False(response.IsValid);
        }

        [Fact]
        public async Task CalculateDiscounts_ScopedCoupon_YieldsZeroForIneligibleCart()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, _) = await SeedProduct(ctx);

            await ctx.Coupons.AddAsync(NewCoupon("SCOPED50", "percentage", 50m,
                c => c.ApplicableProductIds = $"[\"{Guid.NewGuid()}\"]"));
            await ctx.SaveChangesAsync();

            var handler = new CalculateDiscountsQueryHandler(ctx, CreateMapper());
            var result = await handler.Handle(new CalculateDiscountsQuery
            {
                Subtotal = 100m,
                CouponCode = "SCOPED50",
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = productId, Quantity = 1, UnitPrice = 100m, LineTotal = 100m }
                }
            });

            Assert.Equal(0m, result.CouponDiscount);
            Assert.Equal(100m, result.FinalTotal);
        }

        // ---------------------------------------------------------------- D-07

        [Theory]
        [InlineData("{\"percentage\": 250}")]
        [InlineData("{\"percentage\": -10}")]
        [InlineData("{\"discountPercentage\": 101}")]
        [InlineData("{\"discountAmount\": -5}")]
        [InlineData("{\"buyQuantity\": 0, \"getQuantity\": 1}")]
        [InlineData("{\"tiers\": [{\"minSpend\": 100, \"discount\": 150}]}")]
        [InlineData("{\"tiers\": [{\"minSpend\": -1, \"discount\": 10}]}")]
        [InlineData("not json at all")]
        [InlineData("{\"percentage\": 20")]
        [InlineData("[1,2,3]")]
        public void CreatePromotionValidator_RejectsMalformedOrOutOfRangeRules(string rulesJson)
        {
            var validator = new CreatePromotionCommandFluentValidator();
            var result = validator.Validate(new Ecommerce.Application.Commands.Admin.CreatePromotionCommand
            {
                Name = "Bad Promo",
                Type = "percentage_discount",
                RulesJson = rulesJson
            });

            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("{\"percentage\": 20}")]
        [InlineData("{\"percentage\": 100}")]
        [InlineData("{\"discountAmount\": 25}")]
        [InlineData("{\"tiers\": [{\"minSpend\": 100, \"discount\": 30, \"discountType\": \"fixed_amount\"}]}")]
        [InlineData("{\"tiers\": [{\"minSpend\": 100, \"discount\": 30}]}")]
        [InlineData("{}")]
        [InlineData("")]
        public void CreatePromotionValidator_AcceptsWellFormedRules(string rulesJson)
        {
            var validator = new CreatePromotionCommandFluentValidator();
            var result = validator.Validate(new Ecommerce.Application.Commands.Admin.CreatePromotionCommand
            {
                Name = "Good Promo",
                Type = "percentage_discount",
                RulesJson = rulesJson
            });

            Assert.True(result.IsValid, string.Join(" | ", result.Errors.Select(e => e.ErrorMessage)));
        }

        [Fact]
        public void CreatePromotionValidator_RejectsUnsupportedType()
        {
            var validator = new CreatePromotionCommandFluentValidator();
            var result = validator.Validate(new Ecommerce.Application.Commands.Admin.CreatePromotionCommand
            {
                Name = "Weird Promo",
                Type = "give_it_all_away",
                RulesJson = "{\"percentage\": 10}"
            });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void CreatePromotionValidator_RejectsEndBeforeStart()
        {
            var validator = new CreatePromotionCommandFluentValidator();
            var now = DateTimeOffset.UtcNow;
            var result = validator.Validate(new Ecommerce.Application.Commands.Admin.CreatePromotionCommand
            {
                Name = "Backwards Promo",
                Type = "percentage_discount",
                RulesJson = "{\"percentage\": 10}",
                StartAt = now,
                EndAt = now.AddHours(-1)
            });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void UpdatePromotionValidator_RejectsOverHundredPercent()
        {
            var validator = new UpdatePromotionCommandFluentValidator();
            var result = validator.Validate(new Ecommerce.Application.Commands.Admin.UpdatePromotionCommand
            {
                Id = Guid.NewGuid(),
                Name = "Bad Update",
                Type = "percentage_discount",
                RulesJson = "{\"percentage\": 250}"
            });

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task Checkout_PersistedOversizedPromotion_ClampsLineDiscountToLineTotal()
        {
            // A 250% rule that predates validation must not drive the line — or the order —
            // below zero at checkout.
            using var ctx = CreateInMemoryContext();
            var (productId, variantId) = await SeedProduct(ctx, price: 100m);

            await ctx.Promotions.AddAsync(new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Legacy 250%",
                Type = "percentage_discount",
                RulesJson = "{\"percentage\": 250}",
                IsActive = true,
                Priority = 10,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            var handler = new CheckoutCommandHandler(
                ctx,
                new Ecommerce.Infrastructure.Services.IdempotencyService(ctx),
                new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher(),
                null,
                new Ecommerce.Infrastructure.Services.PromotionEvaluationService(ctx));

            var orderId = await handler.Handle(new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 }
                }
            });

            var order = await ctx.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId);
            Assert.All(order.Items, item => Assert.True(item.TotalAmount >= 0m));
            Assert.True(order.Subtotal >= 0m);
            Assert.True(order.TotalAmount >= 0m);

            var usage = await ctx.PromotionUsages.FirstOrDefaultAsync(u => u.OrderId == orderId);
            if (usage != null)
            {
                // The recorded usage must never claim more than the line was worth (200).
                Assert.True(usage.DiscountAmount <= 200m);
            }
        }

        // ---------------------------------------------------- shared calculator

        [Fact]
        public void Calculator_NegativeCouponValue_YieldsZero()
        {
            var coupon = NewCoupon("NEG", "fixed_amount", -50m);
            Assert.Equal(0m, CouponDiscountCalculator.CalculateAmount(coupon, 100m));
        }

        [Fact]
        public void Calculator_PercentageAbove100_ClampsToSubtotal()
        {
            var coupon = NewCoupon("OVER", "percentage", 250m);
            Assert.Equal(100m, CouponDiscountCalculator.CalculateAmount(coupon, 100m));
        }

        [Fact]
        public void Calculator_FixedAmountAboveSubtotal_ClampsToSubtotal()
        {
            var coupon = NewCoupon("BIG", "fixed_amount", 500m);
            Assert.Equal(100m, CouponDiscountCalculator.CalculateAmount(coupon, 100m));
        }

        [Fact]
        public void Calculator_FreeShippingCoupon_HasNoLineDiscount()
        {
            var coupon = NewCoupon("SHIP", "free_shipping", 0m);
            var result = CouponDiscountCalculator.Calculate(
                coupon,
                new[] { new CouponLine { ProductId = Guid.NewGuid(), LineTotal = 100m } });

            Assert.True(result.IsApplicable);
            Assert.True(result.IsFreeShipping);
            Assert.Equal(0m, result.DiscountAmount);
        }

        [Fact]
        public void Calculator_ExclusionWinsOverInclusion()
        {
            var productId = Guid.NewGuid();
            var coupon = NewCoupon("BOTH", "percentage", 50m, c =>
            {
                c.ApplicableProductIds = $"[\"{productId}\"]";
                c.ExcludedProductIds = $"[\"{productId}\"]";
            });

            Assert.False(CouponDiscountCalculator.IsLineEligible(coupon, productId, null));
        }

        [Fact]
        public void Calculator_CartLevelDiscount_LowersCouponCeiling()
        {
            var coupon = NewCoupon("FLAT100", "fixed_amount", 100m);
            var result = CouponDiscountCalculator.Calculate(
                coupon,
                new[] { new CouponLine { ProductId = Guid.NewGuid(), LineTotal = 100m } },
                cartLevelDiscount: 60m);

            // Only 40 is still payable, so the coupon cannot take more than 40.
            Assert.Equal(40m, result.DiscountAmount);
        }

        [Fact]
        public void Calculator_ParsesCsvScopingColumns()
        {
            var productId = Guid.NewGuid();
            var coupon = NewCoupon("CSV", "percentage", 10m,
                c => c.ApplicableProductIds = $"{productId},{Guid.NewGuid()}");

            Assert.True(CouponDiscountCalculator.IsLineEligible(coupon, productId, null));
            Assert.False(CouponDiscountCalculator.IsLineEligible(coupon, Guid.NewGuid(), null));
        }
    }
}
