using System;
using System.Threading.Tasks;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.IntegrationTests
{
    public class InventoryReservationIntegrationTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Reserve_ReducesAvailable_OnInMemoryDb()
        {
            using var context = CreateInMemoryContext();

            var item = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductVariantId = Guid.NewGuid(),
                WarehouseId = Guid.NewGuid(),
            };

            // use AddStock to ensure UpdatedAt and QuantityOnHand set via method
            item.AddStock(20);

            await context.InventoryItems.AddAsync(item);
            await context.SaveChangesAsync();

            // Load from db and reserve
            var fromDb = await context.InventoryItems.FirstAsync(i => i.Id == item.Id);
            fromDb.Reserve(5);
            await context.SaveChangesAsync();

            var after = await context.InventoryItems.FirstAsync(i => i.Id == item.Id);

            Assert.Equal(5, after.QuantityReserved);
            Assert.Equal(15, after.Available);
        }
    }
}
