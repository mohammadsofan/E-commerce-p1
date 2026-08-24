using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Carts;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Mappings;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class CartCouponHandlerTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            return config.CreateMapper();
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId { get; }
            public string UserName => "test-user";
            public bool IsAdmin => false;

            public FakeCurrentUserService(Guid userId) => UserId = userId;
        }

        [Fact]
        public async Task ApplyCoupon_PercentageDiscount_NoCap_CalculatesCorrectDiscount()
        {
            // Arrange
            var db = CreateInMemoryContext();
            var mapper = CreateMapper();
            var userId = Guid.NewGuid();
            var user = new FakeCurrentUserService(userId);

            var cart = Cart.Create(userId, null);
            cart.AddItem(Guid.NewGuid(), null, "Luxury Couch", 100m, 2); // Subtotal = 200
            db.Carts.Add(cart);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "SAVE20",
                Description = "20% off",
                Type = "percentage",
                Value = 20m,
                IsActive = true,
                MaxDiscountAmount = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync();

            var handler = new ApplyCouponToCartCommandHandler(db, user, mapper);

            // Act
            var result = await handler.Handle(new ApplyCouponToCartCommand { Code = "save20" });

            // Assert
            Assert.Equal("SAVE20", result.AppliedCouponCode);
            Assert.Equal(200m, result.Subtotal);
            Assert.Equal(40m, result.DiscountAmount);
            Assert.Equal(160m, result.TotalAmount);
        }

        [Fact]
        public async Task ApplyCoupon_PercentageDiscount_WithCap_ClampsToMaxDiscountAmount()
        {
            // Arrange
            var db = CreateInMemoryContext();
            var mapper = CreateMapper();
            var userId = Guid.NewGuid();
            var user = new FakeCurrentUserService(userId);

            var cart = Cart.Create(userId, null);
            cart.AddItem(Guid.NewGuid(), null, "Dining Table", 500m, 1); // Subtotal = 500
            db.Carts.Add(cart);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "HALFPRICE",
                Description = "50% off capped at 50",
                Type = "percentage",
                Value = 50m, // 50% of 500 = 250
                MaxDiscountAmount = 50m, // Capped at 50
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync();

            var handler = new ApplyCouponToCartCommandHandler(db, user, mapper);

            // Act
            var result = await handler.Handle(new ApplyCouponToCartCommand { Code = "HALFPRICE" });

            // Assert
            Assert.Equal("HALFPRICE", result.AppliedCouponCode);
            Assert.Equal(500m, result.Subtotal);
            Assert.Equal(50m, result.DiscountAmount); // Must be clamped to 50 instead of 250
            Assert.Equal(450m, result.TotalAmount);
        }

        [Fact]
        public async Task ApplyCoupon_BelowMinOrderAmount_ThrowsDomainException()
        {
            // Arrange
            var db = CreateInMemoryContext();
            var mapper = CreateMapper();
            var userId = Guid.NewGuid();
            var user = new FakeCurrentUserService(userId);

            var cart = Cart.Create(userId, null);
            cart.AddItem(Guid.NewGuid(), null, "Small Pillow", 30m, 1); // Subtotal = 30
            db.Carts.Add(cart);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "VIP100",
                Description = "Requires $100 min spend",
                Type = "fixed_amount",
                Value = 25m,
                MinOrderAmount = 100m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync();

            var handler = new ApplyCouponToCartCommandHandler(db, user, mapper);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new ApplyCouponToCartCommand { Code = "VIP100" }));

            Assert.Equal("لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون", ex.Message);
        }

        [Fact]
        public async Task ApplyCoupon_FixedAmount_AppliesExactValue()
        {
            // Arrange
            var db = CreateInMemoryContext();
            var mapper = CreateMapper();
            var userId = Guid.NewGuid();
            var user = new FakeCurrentUserService(userId);

            var cart = Cart.Create(userId, null);
            cart.AddItem(Guid.NewGuid(), null, "Office Chair", 150m, 1); // Subtotal = 150
            db.Carts.Add(cart);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "FLAT30",
                Description = "$30 flat off",
                Type = "fixed_amount",
                Value = 30m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync();

            var handler = new ApplyCouponToCartCommandHandler(db, user, mapper);

            // Act
            var result = await handler.Handle(new ApplyCouponToCartCommand { Code = "FLAT30" });

            // Assert
            Assert.Equal("FLAT30", result.AppliedCouponCode);
            Assert.Equal(150m, result.Subtotal);
            Assert.Equal(30m, result.DiscountAmount);
            Assert.Equal(120m, result.TotalAmount);
        }

        [Fact]
        public async Task RemoveCoupon_ClearsDiscountFromCart()
        {
            // Arrange
            var db = CreateInMemoryContext();
            var mapper = CreateMapper();
            var userId = Guid.NewGuid();
            var user = new FakeCurrentUserService(userId);

            var cart = Cart.Create(userId, null);
            cart.AddItem(Guid.NewGuid(), null, "Desk Lamp", 80m, 1);
            cart.ApplyCoupon("DISCOUNT");
            db.Carts.Add(cart);
            await db.SaveChangesAsync();

            var handler = new RemoveCouponFromCartCommandHandler(db, user, mapper);

            // Act
            var result = await handler.Handle(new RemoveCouponFromCartCommand());

            // Assert
            Assert.Null(result.AppliedCouponCode);
            Assert.Equal(0m, result.DiscountAmount);
            Assert.Equal(80m, result.TotalAmount);
        }
    }
}

