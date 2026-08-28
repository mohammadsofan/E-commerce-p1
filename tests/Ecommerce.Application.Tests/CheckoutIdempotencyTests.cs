using System;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class CheckoutIdempotencyTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Checkout_WithSameIdempotencyKey_IsIdempotent()
        {
            using var context = CreateInMemoryContext();

            var variantId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var product = new Ecommerce.Domain.Entities.Product
            {
                Id = productId,
                Name = "Idempotency Product",
                Sku = $"SKU-{productId}",
                BasePrice = 10m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.Products.AddAsync(product);

            var variant = new Ecommerce.Domain.Entities.ProductVariant
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

            var inv = new Ecommerce.Domain.Entities.InventoryItem { Id = variantId, ProductId = productId, ProductVariantId = variantId };
            inv.AddStock(50);
            await context.InventoryItems.AddAsync(inv);
            await context.SaveChangesAsync();

            var idempotency = new IdempotencyService(context);
            var handler = new CheckoutCommandHandler(context, idempotency, new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());

            var key = "idem-123";

            var command = new CheckoutCommand { ExpectedTotal = -1m,
                UserId = Guid.NewGuid(),
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = key,
                Items = { new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 } }
            };

            var first = await handler.Handle(command);
            var second = await handler.Handle(command);

            Assert.Equal(first, second);
            var orders = await context.Orders.ToListAsync();
            Assert.Single(orders);
        }
    }
}


