using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class PromotionEvaluationServiceTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task EvaluateProduct_TargetedPromotion_AppliesOnlyToTargetedProduct()
        {
            using var ctx = CreateInMemoryContext();
            var service = new PromotionEvaluationService(ctx);

            var productAId = Guid.NewGuid();
            var productBId = Guid.NewGuid();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Special 10% Off Product A",
                Type = "percentage",
                RulesJson = "{\"discountPercentage\": 10}",
                IsActive = true,
                Priority = 1,
                ApplicableProductIds = JsonSerializer.Serialize(new[] { productAId.ToString() }),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.Promotions.Add(promo);
            await ctx.SaveChangesAsync();

            // Evaluate Product A (BasePrice 100)
            var evalA = await service.EvaluateProductAsync(productAId, null, 100m);
            Assert.True(evalA.HasActivePromotion);
            Assert.Equal(90m, evalA.PromotionalPrice);
            Assert.Equal(10m, evalA.DiscountAmount);
            Assert.Equal(10, evalA.DiscountPercentage);
            Assert.Equal("خصم 10%", evalA.PromotionBadge);

            // Evaluate Product B (BasePrice 100)
            var evalB = await service.EvaluateProductAsync(productBId, null, 100m);
            Assert.False(evalB.HasActivePromotion);
            Assert.Equal(100m, evalB.PromotionalPrice);
            Assert.Equal(0m, evalB.DiscountAmount);
        }

        [Fact]
        public async Task EvaluateProduct_FixedAmountDiscount_CalculatesCorrectPrice()
        {
            using var ctx = CreateInMemoryContext();
            var service = new PromotionEvaluationService(ctx);

            var productId = Guid.NewGuid();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "25 OFF",
                Type = "fixed_amount",
                RulesJson = "{\"discountAmount\": 25}",
                IsActive = true,
                Priority = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.Promotions.Add(promo);
            await ctx.SaveChangesAsync();

            var eval = await service.EvaluateProductAsync(productId, null, 100m);
            Assert.True(eval.HasActivePromotion);
            Assert.Equal(75m, eval.PromotionalPrice);
            Assert.Equal(25m, eval.DiscountAmount);
            Assert.Equal(25, eval.DiscountPercentage);
            Assert.Equal("وفر 25 ₪", eval.PromotionBadge);
        }

        [Fact]
        public async Task EvaluateProduct_ExpiredPromotion_IsIgnored()
        {
            using var ctx = CreateInMemoryContext();
            var service = new PromotionEvaluationService(ctx);

            var productId = Guid.NewGuid();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Expired Promo",
                Type = "percentage",
                RulesJson = "{\"discountPercentage\": 50}",
                IsActive = true,
                StartAt = DateTimeOffset.UtcNow.AddDays(-10),
                EndAt = DateTimeOffset.UtcNow.AddDays(-1), // Expired
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.Promotions.Add(promo);
            await ctx.SaveChangesAsync();

            var eval = await service.EvaluateProductAsync(productId, null, 100m);
            Assert.False(eval.HasActivePromotion);
            Assert.Equal(100m, eval.PromotionalPrice);
        }

        [Fact]
        public async Task EvaluateProduct_InactivePromotion_IsIgnored()
        {
            using var ctx = CreateInMemoryContext();
            var service = new PromotionEvaluationService(ctx);

            var productId = Guid.NewGuid();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Inactive Promo",
                Type = "percentage",
                RulesJson = "{\"discountPercentage\": 50}",
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.Promotions.Add(promo);
            await ctx.SaveChangesAsync();

            var eval = await service.EvaluateProductAsync(productId, null, 100m);
            Assert.False(eval.HasActivePromotion);
            Assert.Equal(100m, eval.PromotionalPrice);
        }

        [Fact]
        public async Task EvaluateProduct_ExcludedProduct_IsIgnored()
        {
            using var ctx = CreateInMemoryContext();
            var service = new PromotionEvaluationService(ctx);

            var productId = Guid.NewGuid();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Storewide except VIP item",
                Type = "percentage",
                RulesJson = "{\"discountPercentage\": 20}",
                IsActive = true,
                ExcludedProductIds = JsonSerializer.Serialize(new[] { productId.ToString() }),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.Promotions.Add(promo);
            await ctx.SaveChangesAsync();

            var eval = await service.EvaluateProductAsync(productId, null, 100m);
            Assert.False(eval.HasActivePromotion);
            Assert.Equal(100m, eval.PromotionalPrice);
        }

        [Fact]
        public async Task EvaluateProduct_HigherPriority_OverridesLowerPriority()
        {
            using var ctx = CreateInMemoryContext();
            var service = new PromotionEvaluationService(ctx);

            var productId = Guid.NewGuid();

            var promoLow = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "10% Promo",
                Type = "percentage",
                RulesJson = "{\"discountPercentage\": 10}",
                IsActive = true,
                Priority = 5,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var promoHigh = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "30% Flash Sale",
                Type = "percentage",
                RulesJson = "{\"discountPercentage\": 30}",
                IsActive = true,
                Priority = 100, // Higher priority
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.Promotions.AddRange(promoLow, promoHigh);
            await ctx.SaveChangesAsync();

            var eval = await service.EvaluateProductAsync(productId, null, 200m);
            Assert.True(eval.HasActivePromotion);
            Assert.Equal("30% Flash Sale", eval.PromotionName);
            Assert.Equal(140m, eval.PromotionalPrice);
            Assert.Equal(60m, eval.DiscountAmount);
            Assert.Equal(30, eval.DiscountPercentage);
        }
    }
}

