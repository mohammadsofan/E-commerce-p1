using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Checkout;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.IntegrationTests
{
    public class CheckoutIdempotencyIntegrationTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Checkout_With_IdempotencyKey_Is_Idempotent()
        {
            using var ctx = CreateInMemoryContext();

            // seed inventory
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var inventory = new InventoryItem
            {
                // Handler looks up inventory by ProductVariantId via FindAsync(key),
                // set Id to variantId to match that lookup behavior in tests.
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
            var command = new CheckoutCommand
            {
                UserId = userId,
                Currency = "USD",
                ShippingAddress = "Test Address",
                IdempotencyKey = "idem-test-1"
            };
            command.Items.Add(new CheckoutItem { ProductId = productId, ProductVariantId = variantId, Quantity = 2 });

            var orderId1 = await handler.Handle(command);

            // Second call with same idempotency key should return same order id
            var orderId2 = await handler.Handle(command);

            Assert.Equal(orderId1, orderId2);

            var orders = await ctx.Orders.Where(o => o.Id == orderId1).ToListAsync();
            Assert.Single(orders);
        }
    }
}
