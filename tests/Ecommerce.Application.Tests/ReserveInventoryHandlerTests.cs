using System;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.ReserveInventory;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class ReserveInventoryHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Handler_ReservesInventory_WhenSufficient()
        {
            using var context = CreateInMemoryContext();

            var item = new InventoryItem { Id = Guid.NewGuid() };
            item.AddStock(10);
            await context.InventoryItems.AddAsync(item);
            await context.SaveChangesAsync();

            var handler = new ReserveInventoryCommandHandler(context);
            var command = new ReserveInventoryCommand { InventoryItemId = item.Id, Quantity = 4 };

            await handler.Handle(command);

            var updated = await context.InventoryItems.FirstAsync(i => i.Id == item.Id);
            Assert.Equal(4, updated.QuantityReserved);
        }
    }
}

