using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminCouponHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IMapper CreateMapper()
        {
            return new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        [Fact]
        public async Task CreateCoupon_CreatesPercentageCoupon()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateCouponCommandHandler(ctx, CreateMapper());

            var command = new CreateCouponCommand
            {
                Code = "SAVE10",
                Description = "10% off",
                Type = "percentage",
                Value = 10,
                IsActive = true,
                AllowCombine = false,
                UsageLimit = 100,
                PerUserLimit = 1,
                MinOrderAmount = 50m
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal("SAVE10", result.Code);
            Assert.Equal("percentage", result.Type);
            Assert.Equal(10, result.Value);
            Assert.True(result.IsActive);
            Assert.Equal(100, result.UsageLimit);
            Assert.Equal(1, result.PerUserLimit);
            Assert.Equal(50m, result.MinOrderAmount);
        }

        [Fact]
        public async Task CreateCoupon_CreatesFixedAmountCoupon()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateCouponCommandHandler(ctx, CreateMapper());

            var command = new CreateCouponCommand
            {
                Code = "FLAT20",
                Description = "$20 off",
                Type = "fixed_amount",
                Value = 20,
                IsActive = true,
                MaxDiscountAmount = 20
            };

            var result = await handler.Handle(command);

            Assert.Equal("fixed_amount", result.Type);
            Assert.Equal(20, result.Value);
        }

        [Fact]
        public async Task CreateCoupon_DuplicateCode_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateCouponCommandHandler(ctx, CreateMapper());

            var command1 = new CreateCouponCommand { Code = "DUPLICATE", Type = "percentage", Value = 10, IsActive = true };
            await handler.Handle(command1);

            var command2 = new CreateCouponCommand { Code = "duplicate", Type = "percentage", Value = 20, IsActive = true };
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command2));
        }

        [Fact]
        public async Task UpdateCoupon_UpdatesFields()
        {
            using var ctx = CreateInMemoryContext();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "OLDCODE",
                Description = "Old",
                Type = "percentage",
                Value = 5,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var handler = new UpdateCouponCommandHandler(ctx, CreateMapper());
            var command = new UpdateCouponCommand
            {
                Id = coupon.Id,
                Code = "NEWCODE",
                Description = "Updated",
                Type = "fixed_amount",
                Value = 15,
                IsActive = false,
                RowVersion = coupon.RowVersion
            };

            var result = await handler.Handle(command);

            Assert.Equal("NEWCODE", result.Code);
            Assert.Equal("Updated", result.Description);
            Assert.Equal("fixed_amount", result.Type);
            Assert.Equal(15, result.Value);
            Assert.False(result.IsActive);
        }

        [Fact]
        public async Task DeleteCoupon_UsedCoupon_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "USED",
                Type = "percentage",
                Value = 10,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);

            var usage = new CouponUsage
            {
                Id = Guid.NewGuid(),
                CouponId = coupon.Id,
                UserId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                DiscountAmount = 5,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.CouponUsages.AddAsync(usage);
            await ctx.SaveChangesAsync();

            var handler = new DeleteCouponCommandHandler(ctx);
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new DeleteCouponCommand { Id = coupon.Id }));
        }

        [Fact]
        public async Task ValidateCoupon_ValidCoupon_ReturnsDiscount()
        {
            using var ctx = CreateInMemoryContext();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "VALID10",
                Type = "percentage",
                Value = 10,
                IsActive = true,
                MinOrderAmount = 50,
                MaxDiscountAmount = 100,
                UsageLimit = 100,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var queryHandler = new ValidateCouponQueryHandler(ctx, CreateMapper());
            var query = new ValidateCouponQuery
            {
                Code = "VALID10",
                UserId = Guid.NewGuid(),
                OrderTotal = 100m
            };

            var result = await queryHandler.Handle(query);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Coupon);
            Assert.Equal(10m, result.DiscountAmount); // 10% of 100
        }

        [Fact]
        public async Task ValidateCoupon_ExpiredCoupon_ReturnsInvalid()
        {
            using var ctx = CreateInMemoryContext();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "EXPIRED",
                Type = "percentage",
                Value = 10,
                IsActive = true,
                EndAt = DateTimeOffset.UtcNow.AddDays(-1),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var queryHandler = new ValidateCouponQueryHandler(ctx, CreateMapper());
            var query = new ValidateCouponQuery { Code = "EXPIRED", UserId = Guid.NewGuid(), OrderTotal = 100m };

            var result = await queryHandler.Handle(query);

            Assert.False(result.IsValid);
            Assert.Equal("Coupon has expired", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateCoupon_BelowMinOrder_ReturnsInvalid()
        {
            using var ctx = CreateInMemoryContext();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "MIN50",
                Type = "percentage",
                Value = 10,
                IsActive = true,
                MinOrderAmount = 100,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var queryHandler = new ValidateCouponQueryHandler(ctx, CreateMapper());
            var query = new ValidateCouponQuery { Code = "MIN50", UserId = Guid.NewGuid(), OrderTotal = 50m };

            var result = await queryHandler.Handle(query);

            Assert.False(result.IsValid);
            Assert.Contains("Minimum order", result.ErrorMessage);
        }

        [Fact]
        public async Task CalculateDiscounts_AppliesCoupon()
        {
            using var ctx = CreateInMemoryContext();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "TEST20",
                Type = "percentage",
                Value = 20,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Coupons.AddAsync(coupon);
            await ctx.SaveChangesAsync();

            var queryHandler = new CalculateDiscountsQueryHandler(ctx, CreateMapper());
            var query = new CalculateDiscountsQuery
            {
                UserId = Guid.NewGuid(),
                Subtotal = 100m,
                CouponCode = "TEST20",
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100m }
                }
            };

            var result = await queryHandler.Handle(query);

            Assert.Equal(100m, result.Subtotal);
            Assert.Equal(20m, result.CouponDiscount);
            Assert.Equal(20m, result.TotalDiscount);
            Assert.Equal(80m, result.FinalTotal);
            Assert.Single(result.AppliedDiscounts);
            Assert.Equal("coupon", result.AppliedDiscounts[0].Type);
        }
    }
}