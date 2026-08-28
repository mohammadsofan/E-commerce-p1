using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
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

            var product = new Product
            {
                Id = productId,
                Name = "Variant Product",
                Sku = $"SKU-{productId}",
                BasePrice = 10m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Products.AddAsync(product);

            var variant = new ProductVariant
            {
                Id = variantId,
                ProductId = productId,
                Name = "Variant A",
                Sku = $"VAR-{variantId}",
                Price = 10m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.ProductVariants.AddAsync(variant);

            var inv = new InventoryItem { Id = variantId, ProductId = productId, ProductVariantId = variantId };
            inv.AddStock(50);
            await context.InventoryItems.AddAsync(inv);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var command = new CheckoutCommand { ExpectedTotal = -1m,
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

            var product = new Product
            {
                Id = productId,
                Name = "Coupon Product",
                Sku = $"SKU-{productId}",
                BasePrice = 50m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Products.AddAsync(product);

            var inv = new InventoryItem { Id = productId, ProductId = productId };
            inv.AddStock(20);
            await context.InventoryItems.AddAsync(inv);

            var cart = Cart.Create(userId, null);
            cart.AddItem(productId, null, "Item A", 50m, 2);
            cart.ApplyCoupon("SAVE10");
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

            var command = new CheckoutCommand { ExpectedTotal = -1m,
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
        }

        [Fact]
        public async Task Checkout_WithExpiredCouponInCart_ThrowsException()
        {
            using var context = CreateInMemoryContext();

            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Expired Coupon Product",
                Sku = $"SKU-{productId}",
                BasePrice = 100m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Products.AddAsync(product);

            var inv = new InventoryItem { Id = productId, ProductId = productId };
            inv.AddStock(10);
            await context.InventoryItems.AddAsync(inv);

            var cart = Cart.Create(userId, null);
            cart.AddItem(productId, null, "Sofa", 100m, 1);
            cart.ApplyCoupon("EXPIRED");
            await context.Carts.AddAsync(cart);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "EXPIRED",
                Description = "Expired promo",
                Type = "fixed_amount",
                Value = 20m,
                IsActive = true,
                EndAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Coupons.AddAsync(coupon);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = Guid.NewGuid().ToString(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, Quantity = 1 }
                }
            };

            var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));
            Assert.Equal("انتهت صلاحية الكوبون", ex.Message);

            // Verify that the invalid coupon was stripped from the cart in database
            var updatedCart = await context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            Assert.NotNull(updatedCart);
            Assert.Null(updatedCart.AppliedCouponCode);
        }

        [Fact]
        public async Task Checkout_WhenSubtotalBelowFreeShippingThreshold_AppliesStandardShipping()
        {
            using var context = CreateInMemoryContext();

            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Sofa Cushion",
                Sku = "CUSH-01",
                BasePrice = 30m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Products.AddAsync(product);

            var inv = new InventoryItem { Id = productId, ProductId = productId };
            inv.AddStock(10);
            await context.InventoryItems.AddAsync(inv);

            var setting = new StoreSetting
            {
                StandardShippingCost = 15m,
                FreeShippingThreshold = 50m
            };
            await context.StoreSettings.AddAsync(setting);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                Currency = "ILS",
                ShippingAddress = "Test Address",
                IdempotencyKey = Guid.NewGuid().ToString(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, Quantity = 1 }
                }
            };

            var orderId = await handler.Handle(command);
            var order = await context.Orders.FindAsync(orderId);

            Assert.NotNull(order);
            Assert.Equal(30m, order.Subtotal);
            Assert.Equal(15m, order.ShippingAmount);
            Assert.Equal(45m, order.TotalAmount);
        }

        [Fact]
        public async Task Checkout_WhenSubtotalMeetsFreeShippingThreshold_AppliesFreeShipping()
        {
            using var context = CreateInMemoryContext();

            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Luxury Couch",
                Sku = "COUCH-01",
                BasePrice = 80m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Products.AddAsync(product);

            var inv = new InventoryItem { Id = productId, ProductId = productId };
            inv.AddStock(10);
            await context.InventoryItems.AddAsync(inv);

            var setting = new StoreSetting
            {
                StandardShippingCost = 15m,
                FreeShippingThreshold = 50m
            };
            await context.StoreSettings.AddAsync(setting);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                Currency = "ILS",
                ShippingAddress = "Test Address",
                IdempotencyKey = Guid.NewGuid().ToString(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, Quantity = 1 }
                }
            };

            var orderId = await handler.Handle(command);
            var order = await context.Orders.FindAsync(orderId);

            Assert.NotNull(order);
            Assert.Equal(80m, order.Subtotal);
            Assert.Equal(0m, order.ShippingAmount);
            Assert.Equal(80m, order.TotalAmount);
        }

        [Fact]
        public async Task Checkout_WithFreeShippingCoupon_AppliesFreeShipping()
        {
            using var context = CreateInMemoryContext();

            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Table Lamp",
                Sku = "LAMP-01",
                BasePrice = 25m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Products.AddAsync(product);

            var inv = new InventoryItem { Id = productId, ProductId = productId };
            inv.AddStock(10);
            await context.InventoryItems.AddAsync(inv);

            var setting = new StoreSetting
            {
                StandardShippingCost = 15m,
                FreeShippingThreshold = 50m
            };
            await context.StoreSettings.AddAsync(setting);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "FREESHIP",
                Description = "Free delivery",
                Type = "free_shipping",
                Value = 0m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Coupons.AddAsync(coupon);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                CouponCode = "FREESHIP",
                Currency = "ILS",
                ShippingAddress = "Test Address",
                IdempotencyKey = Guid.NewGuid().ToString(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, Quantity = 1 }
                }
            };

            var orderId = await handler.Handle(command);
            var order = await context.Orders.FindAsync(orderId);

            Assert.NotNull(order);
            Assert.Equal(25m, order.Subtotal);
            Assert.Equal(0m, order.ShippingAmount);
            Assert.Equal(25m, order.TotalAmount);
        }

        [Fact]
        public async Task Checkout_AllocatesAcrossMultipleWarehouses_WhenSingleWarehouseInsufficient()
        {
            using var context = CreateInMemoryContext();

            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "MultiWarehouse Product",
                Slug = "multi-wh-prod",
                BasePrice = 50m,
                CurrencyCode = "USD",
                Status = "Active",
                IsActive = true
            };
            await context.Products.AddAsync(product);

            var warehouse1Id = Guid.NewGuid();
            var warehouse2Id = Guid.NewGuid();

            // Warehouse 1 has 3 items
            var inv1 = new InventoryItem(productId, warehouse1Id, quantityOnHand: 3);
            // Warehouse 2 has 4 items
            var inv2 = new InventoryItem(productId, warehouse2Id, quantityOnHand: 4);

            await context.InventoryItems.AddRangeAsync(inv1, inv2);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            // Order requires 5 items (neither warehouse has 5 alone, but together they have 7)
            var command = new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = Guid.NewGuid().ToString(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, Quantity = 5 }
                }
            };

            var orderId = await handler.Handle(command);

            var order = await context.Orders.FindAsync(orderId);
            Assert.NotNull(order);

            var updatedInv1 = await context.InventoryItems.FindAsync(inv1.Id);
            var updatedInv2 = await context.InventoryItems.FindAsync(inv2.Id);

            // Total reserved across both warehouses must equal 5
            Assert.Equal(5, updatedInv1!.QuantityReserved + updatedInv2!.QuantityReserved);
            // Greedy allocation picked inv2 (4 available) first and reserved 4, then inv1 reserved 1
            Assert.Equal(4, updatedInv2.QuantityReserved);
            Assert.Equal(1, updatedInv1.QuantityReserved);
        }

        [Fact]
        public async Task Checkout_ThrowsDomainException_WhenTotalStockAcrossAllWarehousesInsufficient()
        {
            using var context = CreateInMemoryContext();

            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Short Stock Product",
                Slug = "short-stock",
                BasePrice = 30m,
                CurrencyCode = "USD",
                Status = "Active",
                IsActive = true
            };
            await context.Products.AddAsync(product);

            var warehouse1Id = Guid.NewGuid();
            var warehouse2Id = Guid.NewGuid();

            // Total available across all warehouses is 2 + 2 = 4
            var inv1 = new InventoryItem(productId, warehouse1Id, quantityOnHand: 2);
            var inv2 = new InventoryItem(productId, warehouse2Id, quantityOnHand: 2);

            await context.InventoryItems.AddRangeAsync(inv1, inv2);
            await context.SaveChangesAsync();

            var idempotency = new Ecommerce.Infrastructure.Services.IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            // Requesting 5 items when only 4 are available
            var command = new CheckoutCommand
            {
                ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = Guid.NewGuid().ToString(),
                Items = new List<CheckoutItem>
                {
                    new CheckoutItem { ProductId = productId, Quantity = 5 }
                }
            };

            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));
        }
    }
}



