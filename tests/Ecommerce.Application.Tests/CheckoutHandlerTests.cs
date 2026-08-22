using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class CheckoutHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Checkout_CreatesOrder_AndReservesInventory()
        {
            using var context = CreateInMemoryContext();

            var variantId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var inv = new InventoryItem { Id = variantId, ProductId = productId, ProductVariantId = variantId };
            inv.AddStock(50);
            await context.InventoryItems.AddAsync(inv);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var command = new CheckoutCommand
            {
                UserId = Guid.NewGuid(),
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = "test-key",
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 3 }
                }
            };

            var orderId = await handler.Handle(command);

            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            Assert.NotNull(order);
            var updatedInv = await context.InventoryItems.FirstAsync(i => i.Id == variantId);
            Assert.Equal(3, updatedInv.QuantityReserved);
        }

        [Fact]
        public async Task Checkout_WithAppliedCoupon_ClearsUserCartAndCoupon()
        {
            using var context = CreateInMemoryContext();

            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var inv = new InventoryItem { Id = productId, ProductId = productId };
            inv.AddStock(20);
            await context.InventoryItems.AddAsync(inv);

            var cart = Cart.Create(userId, null);
            cart.AddItem(productId, null, "Item A", 50m, 2);
            cart.ApplyCoupon("SAVE10", 10m);
            await context.Carts.AddAsync(cart);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "SAVE10",
                Description = "10 off",
                Type = "fixed_amount",
                Value = 10m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Coupons.AddAsync(coupon);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var command = new CheckoutCommand
            {
                UserId = userId,
                CouponCode = "SAVE10",
                Currency = "USD",
                ShippingAddress = "Test",
                IdempotencyKey = Guid.NewGuid().ToString(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, Quantity = 2 }
                }
            };

            var orderId = await handler.Handle(command);

            Assert.NotEqual(Guid.Empty, orderId);

            var userCart = await context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            Assert.NotNull(userCart);
            Assert.Empty(userCart.Items);
            Assert.Null(userCart.AppliedCouponCode);
            Assert.Equal(0m, userCart.DiscountAmount);
            Assert.Equal(0m, userCart.TotalAmount);
        }
    }
}
