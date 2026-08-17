using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminInventoryHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static (Guid productId, Guid variantId, Guid warehouseId, InventoryItem inventory) CreateTestData()
        {
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();

            var inventory = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ProductVariantId = Guid.NewGuid(),
                WarehouseId = Guid.NewGuid(),
                ReorderLevel = 5,
                ReorderQuantity = 20,
                AllowBackorder = false
            };
            inventory.AddStock(10);

            return (productId, variantId, warehouseId, inventory);
        }

        private static (Guid productId, Guid variantId, Guid warehouseId, InventoryItem inventory) CreateTestDataWithStock(int stock)
        {
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();

            var inventory = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductVariantId = Guid.NewGuid(),
                WarehouseId = Guid.NewGuid(),
                ReorderLevel = 5,
                ReorderQuantity = 20,
                AllowBackorder = false
            };
            inventory.AddStock(stock);

            return (productId, variantId, warehouseId, inventory);
        }

        private static async Task SeedProductVariantWarehouse(ApplicationDbContext ctx, Guid productId, Guid variantId, Guid warehouseId)
        {
            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TEST-001",
                BasePrice = 100m,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Sku = "TEST-VAR-001",
                Name = "Test Variant",
                Price = 100m,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductVariants.AddAsync(variant);

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = "Main Warehouse",
                Code = "WH-001",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Warehouses.AddAsync(warehouse);
        }

        [Fact]
        public async Task AdjustInventory_AddsStock()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId, warehouseId, inventory) = CreateTestData();

            await SeedProductVariantWarehouse(ctx, productId, variantId, warehouseId);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var handler = new AdjustInventoryCommandHandler(ctx);

            var command = new AdjustInventoryCommand
            {
                InventoryItemId = inventory.Id,
                QuantityChange = 5,
                Reason = "Restock"
            };

            await handler.Handle(command);

            var updated = await ctx.InventoryItems.FindAsync(inventory.Id);
            Assert.Equal(15, updated.QuantityOnHand);
            Assert.Equal(15, updated.Available);
        }

        [Fact]
        public async Task AdjustInventory_RemovesStock()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId, warehouseId, inventory) = CreateTestDataWithStock(15);

            await SeedProductVariantWarehouse(ctx, productId, variantId, warehouseId);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var handler = new AdjustInventoryCommandHandler(ctx);

            var command = new AdjustInventoryCommand
            {
                InventoryItemId = inventory.Id,
                QuantityChange = -5,
                Reason = "Sale"
            };

            await handler.Handle(command);

            var updated = await ctx.InventoryItems.FindAsync(inventory.Id);
            Assert.Equal(10, updated.QuantityOnHand);
            Assert.Equal(10, updated.Available);
        }

        [Fact]
        public async Task AdjustInventory_InsufficientStock_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId, warehouseId, inventory) = CreateTestDataWithStock(3);

            await SeedProductVariantWarehouse(ctx, productId, variantId, warehouseId);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var handler = new AdjustInventoryCommandHandler(ctx);

            var command = new AdjustInventoryCommand
            {
                InventoryItemId = inventory.Id,
                QuantityChange = -5, // More than available
                Reason = "Sale"
            };

            await Assert.ThrowsAsync<InventoryException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task SetReorderPoint_UpdatesValues()
        {
            using var ctx = CreateInMemoryContext();
            var (productId, variantId, warehouseId, inventory) = CreateTestData();

            await SeedProductVariantWarehouse(ctx, productId, variantId, warehouseId);
            await ctx.InventoryItems.AddAsync(inventory);
            await ctx.SaveChangesAsync();

            var handler = new SetReorderPointCommandHandler(ctx);

            var command = new SetReorderPointCommand
            {
                InventoryItemId = inventory.Id,
                ReorderLevel = 10,
                ReorderQuantity = 30
            };

            await handler.Handle(command);

            var updated = await ctx.InventoryItems.FindAsync(inventory.Id);
            Assert.Equal(10, updated.ReorderLevel);
            Assert.Equal(30, updated.ReorderQuantity);
        }
    }
}