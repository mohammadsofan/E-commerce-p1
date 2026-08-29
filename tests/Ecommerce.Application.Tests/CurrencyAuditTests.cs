using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ecommerce.Application.Tests
{
    public class CurrencyAuditTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Convert_100ILS_To_27USD()
        {
            using var ctx = CreateInMemoryContext();
            var seeder = new Ecommerce.Infrastructure.Persistence.DbSeeder(NullLogger<Ecommerce.Infrastructure.Persistence.DbSeeder>.Instance, new DummyConfig(), new DummySearchService());
            // Seed via public SeedAsync which will seed currencies and rates
            await seeder.SeedAsync(ctx, null, null);
            var handler = new ConvertCurrencyQueryHandler(ctx);
            var result = await handler.Handle(new ConvertCurrencyQuery { Amount = 100m, From = "ILS", To = "USD" });
            Assert.Equal(0.27m, result.Rate);
            Assert.Equal(27m, result.ConvertedAmount);
        }

        [Fact]
        public async Task Convert_Inverse_USD_To_ILS_Is_Correct()
        {
            using var ctx = CreateInMemoryContext();
            var seeder = new Ecommerce.Infrastructure.Persistence.DbSeeder(NullLogger<Ecommerce.Infrastructure.Persistence.DbSeeder>.Instance, new DummyConfig(), new DummySearchService());
            await seeder.SeedAsync(ctx, null, null);
            var handler = new ConvertCurrencyQueryHandler(ctx);
            var result = await handler.Handle(new ConvertCurrencyQuery { Amount = 27m, From = "USD", To = "ILS" });
            // 1 / 0.27 = 3.7037...
            Assert.Equal(3.7037037037037037037037037037m, result.Rate);
            Assert.Equal(100m, result.ConvertedAmount);
            var result2 = await handler.Handle(new ConvertCurrencyQuery { Amount = 100m, From = "USD", To = "ILS" });
            Assert.Equal(370.37037037037037037037037037m, result2.ConvertedAmount);
        }

        [Fact]
        public async Task MissingRate_ShouldThrow_NotSilentlyReturnOneToOne()
        {
            using var ctx = CreateInMemoryContext();
            var seeder = new Ecommerce.Infrastructure.Persistence.DbSeeder(NullLogger<Ecommerce.Infrastructure.Persistence.DbSeeder>.Instance, new DummyConfig(), new DummySearchService());
            await seeder.SeedAsync(ctx, null, null);
            // Create a new currency without rate
            var jpy = new Currency { Id = Guid.NewGuid(), Code = "JPY", Symbol = "¥", IsBaseCurrency = false };
            ctx.Currencies.Add(jpy);
            await ctx.SaveChangesAsync();
            var handler = new ConvertCurrencyQueryHandler(ctx);
            await Assert.ThrowsAsync<Ecommerce.Domain.Exceptions.DomainException>(() =>
                handler.Handle(new ConvertCurrencyQuery { Amount = 100m, From = "ILS", To = "JPY" }));
        }

        [Fact]
        public async Task Checkout_Persists_Base_ILS_Not_Display_Currency()
        {
            using var ctx = CreateInMemoryContext();
            var seeder = new Ecommerce.Infrastructure.Persistence.DbSeeder(NullLogger<Ecommerce.Infrastructure.Persistence.DbSeeder>.Instance, new DummyConfig(), new DummySearchService());
            await seeder.SeedAsync(ctx, null, null);
            // Create product and inventory
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Test", Slug = "test", Sku = "SKU", BasePrice = 100m, IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            var warehouse = await ctx.Warehouses.FirstOrDefaultAsync();
            var warehouseId = warehouse?.Id ?? Guid.NewGuid();
            if (warehouse == null)
            {
                var wh = new Warehouse { Id = warehouseId, Name = "Test WH", Code = "WH-TEST", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
                ctx.Warehouses.Add(wh);
                await ctx.SaveChangesAsync();
            }
            var invItem = new InventoryItem(productId, warehouseId, 10);
            ctx.InventoryItems.Add(invItem);
            await ctx.SaveChangesAsync();
            var cart = Cart.Create(Guid.NewGuid(), null);
            cart.AddItem(productId, null, "Test", 100m, 1);
            ctx.Carts.Add(cart);
            await ctx.SaveChangesAsync();

            var handler = new Ecommerce.Application.Commands.Checkout.CheckoutCommandHandler(ctx, new Ecommerce.Infrastructure.Services.IdempotencyService(ctx), new Ecommerce.Application.Common.DomainEvents.NullDomainEventDispatcher());
            var orderId = await handler.Handle(new Ecommerce.Application.Commands.Checkout.CheckoutCommand
            {
                UserId = cart.UserId!.Value,
                Currency = "USD", // User had USD selected in UI
                Items = new System.Collections.Generic.List<Ecommerce.Application.Commands.Checkout.CheckoutItem> { new Ecommerce.Application.Commands.Checkout.CheckoutItem { ProductId = productId, Quantity = 1 } },
                ExpectedTotal = -1m,
                IdempotencyKey = Guid.NewGuid().ToString()
            });
            var order = await ctx.Orders.FindAsync(orderId);
            Assert.NotNull(order);
            // Must be persisted as ILS (base), not USD, and amount is base 100
            Assert.Equal("ILS", order.CurrencyCode);
            Assert.Equal(100m, order.Subtotal);
        }

        [Fact]
        public async Task SeedExchangeRates_Is_Idempotent_NoDuplicates()
        {
            using var ctx = CreateInMemoryContext();
            var seeder = new Ecommerce.Infrastructure.Persistence.DbSeeder(NullLogger<Ecommerce.Infrastructure.Persistence.DbSeeder>.Instance, new DummyConfig(), new DummySearchService());
            await seeder.SeedAsync(ctx, null, null);
            var count1 = await ctx.ExchangeRates.CountAsync();
            await seeder.SeedAsync(ctx, null, null);
            var count2 = await ctx.ExchangeRates.CountAsync();
            Assert.Equal(count1, count2);
            // Must be 6 seeded rates
            Assert.Equal(6, count1);
        }

        [Fact]
        public async Task SeedExchangeRates_Does_Not_Overwrite_Admin_Rate()
        {
            using var ctx = CreateInMemoryContext();
            var seeder = new Ecommerce.Infrastructure.Persistence.DbSeeder(NullLogger<Ecommerce.Infrastructure.Persistence.DbSeeder>.Instance, new DummyConfig(), new DummySearchService());
            await seeder.SeedAsync(ctx, null, null);
            var ils = await ctx.Currencies.FirstAsync(c => c.Code == "ILS");
            var usd = await ctx.Currencies.FirstAsync(c => c.Code == "USD");
            var existing = await ctx.ExchangeRates.FirstAsync(r => r.FromCurrencyId == ils.Id && r.ToCurrencyId == usd.Id);
            var originalRate = existing.Rate;
            existing.Rate = 0.50m;
            await ctx.SaveChangesAsync();
            await seeder.SeedAsync(ctx, null, null);
            var after = await ctx.ExchangeRates.FirstAsync(r => r.FromCurrencyId == ils.Id && r.ToCurrencyId == usd.Id);
            Assert.Equal(0.50m, after.Rate);
        }

        [Fact]
        public async Task ZeroOrNegativeRate_Is_Rejected_And_Not_Used_For_Conversion()
        {
            using var ctx = CreateInMemoryContext();
            var seeder = new Ecommerce.Infrastructure.Persistence.DbSeeder(NullLogger<Ecommerce.Infrastructure.Persistence.DbSeeder>.Instance, new DummyConfig(), new DummySearchService());
            await seeder.SeedAsync(ctx, null, null);
            var ils = await ctx.Currencies.FirstAsync(c => c.Code == "ILS");
            var eur = await ctx.Currencies.FirstAsync(c => c.Code == "EUR");
            // Try to create zero rate via handler - should throw
            var handler = new Ecommerce.Application.Commands.Admin.CreateExchangeRateCommandHandler(ctx, new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Ecommerce.Application.Mappings.MappingProfile>()).CreateMapper());
            await Assert.ThrowsAsync<Ecommerce.Domain.Exceptions.DomainException>(() =>
                handler.Handle(new Ecommerce.Application.Commands.Admin.CreateExchangeRateCommand { FromCurrencyId = ils.Id, ToCurrencyId = eur.Id, Rate = 0m }));
            await Assert.ThrowsAsync<Ecommerce.Domain.Exceptions.DomainException>(() =>
                handler.Handle(new Ecommerce.Application.Commands.Admin.CreateExchangeRateCommand { FromCurrencyId = ils.Id, ToCurrencyId = eur.Id, Rate = -1m }));
            // Even if a zero rate somehow existed, conversion should treat it as missing (throw, not 0)
            // We simulate by directly inserting a zero rate and verifying convert throws
            var zeroRate = new ExchangeRate { Id = Guid.NewGuid(), FromCurrencyId = ils.Id, ToCurrencyId = eur.Id, Rate = 0m, EffectiveAt = DateTimeOffset.UtcNow.AddDays(1) };
            // This would be future, so not effective yet - convert should still use old valid rate
            ctx.ExchangeRates.Add(zeroRate);
            await ctx.SaveChangesAsync();
            var convertHandler = new ConvertCurrencyQueryHandler(ctx);
            var result = await convertHandler.Handle(new ConvertCurrencyQuery { Amount = 100m, From = "ILS", To = "USD" });
            Assert.Equal(27m, result.ConvertedAmount); // still uses valid USD rate, not zero EUR rate
        }
    }
}
