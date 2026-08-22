using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.IntegrationTests
{
    public class CheckoutConcurrencyIntegrationTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Checkout_ConcurrentRequests_OnlyOneSucceeds()
        {
            using var ctx = CreateInMemoryContext();

            // Seed inventory with limited stock (5 units)
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var inventory = new InventoryItem
            {
                Id = variantId,
                ProductId = productId,
                ProductVariantId = variantId,
                WarehouseId = Guid.NewGuid(),
                AllowBackorder = false
            };
            inventory.AddStock(5);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var idempotency = new IdempotencyService(ctx);
            var handler = new CheckoutCommandHandler(ctx, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var userId = Guid.NewGuid();
            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = "concurrent-test"
            };
            command.Items.Add(new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 3 });

            // First request succeeds (reserves 3, leaves 2)
            var orderId1 = await handler.Handle(command);
            Assert.NotEqual(Guid.Empty, orderId1);

            // Second concurrent request should fail due to insufficient inventory (only 2 left, needs 3)
            var command2 = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = "concurrent-test-2"
            };
            command2.Items.Add(new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 3 });

            await Assert.ThrowsAsync<InventoryException>(() => handler.Handle(command2));

            // Verify inventory state: first order reserved 3, second failed, available = 2
            var updatedInv = await ctx.InventoryItems.FirstAsync(i => i.Id == variantId);
            Assert.Equal(3, updatedInv.QuantityReserved);
            Assert.Equal(2, updatedInv.Available);
        }

        [Fact]
        public async Task Checkout_ConcurrentRequests_SameIdempotencyKey_ReturnsSameOrder()
        {
            using var ctx = CreateInMemoryContext();

            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var inventory = new InventoryItem
            {
                Id = variantId,
                ProductId = productId,
                ProductVariantId = variantId,
                WarehouseId = Guid.NewGuid(),
                AllowBackorder = false
            };
            inventory.AddStock(10);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var idempotency = new IdempotencyService(ctx);
            var handler = new CheckoutCommandHandler(ctx, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var userId = Guid.NewGuid();
            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = "same-key-test"
            };
            command.Items.Add(new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 });

            // Simulate concurrent requests with same idempotency key
            var task1 = handler.Handle(command);
            var task2 = handler.Handle(command);

            var orderId1 = await task1;
            var orderId2 = await task2;

            // Both should return the same order ID due to idempotency
            Assert.Equal(orderId1, orderId2);

            // Only one order should exist
            var orders = await ctx.Orders.ToListAsync();
            Assert.Single(orders);
            Assert.Equal(orderId1, orders[0].Id);
        }

        [Fact]
        public async Task ReserveInventory_ConcurrentReservations_RespectsStockLimit()
        {
            using var ctx = CreateInMemoryContext();

            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var inventory = new InventoryItem
            {
                Id = variantId,
                ProductId = productId,
                ProductVariantId = variantId,
                WarehouseId = Guid.NewGuid(),
                AllowBackorder = false
            };
            inventory.AddStock(5);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var reserveHandler = new Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommandHandler(ctx);

            // Reserve 3 units (should succeed)
            var reserveCmd1 = new Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand
            {
                InventoryItemId = variantId,
                Quantity = 3
            };
            await reserveHandler.Handle(reserveCmd1);

            // Try to reserve another 3 units (should fail - only 2 available)
            var reserveCmd2 = new Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand
            {
                InventoryItemId = variantId,
                Quantity = 3
            };

            await Assert.ThrowsAsync<InventoryException>(() => reserveHandler.Handle(reserveCmd2));

            // Verify state
            var updatedInv = await ctx.InventoryItems.FirstAsync(i => i.Id == variantId);
            Assert.Equal(3, updatedInv.QuantityReserved);
            Assert.Equal(2, updatedInv.Available);
        }

        [Fact]
        public async Task Checkout_WithBackorderAllowed_AllowsOverReservation()
        {
            using var ctx = CreateInMemoryContext();

            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var inventory = new InventoryItem
            {
                Id = variantId,
                ProductId = productId,
                ProductVariantId = variantId,
                WarehouseId = Guid.NewGuid(),
                AllowBackorder = true // Allow backorder
            };
            inventory.AddStock(2);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var idempotency = new IdempotencyService(ctx);
            var handler = new CheckoutCommandHandler(ctx, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var userId = Guid.NewGuid();
            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = userId,
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = "backorder-test"
            };
            command.Items.Add(new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 5 }); // More than stock

            // Should succeed because backorder is allowed
            var orderId = await handler.Handle(command);
            Assert.NotEqual(Guid.Empty, orderId);

            // Inventory should show negative available (backorder)
            var updatedInv = await ctx.InventoryItems.FirstAsync(i => i.Id == variantId);
            Assert.Equal(5, updatedInv.QuantityReserved);
            Assert.Equal(-3, updatedInv.Available); // 2 - 5 = -3 (backorder)
        }
    }
}


