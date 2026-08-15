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

            var handler = new CheckoutCommandHandler(context);

            var command = new CheckoutCommand
            {
                UserId = Guid.NewGuid(),
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
    }
}
