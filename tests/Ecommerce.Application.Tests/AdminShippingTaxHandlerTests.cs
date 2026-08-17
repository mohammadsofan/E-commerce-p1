using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminShippingTaxHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task ShippingZone_CanBeCreatedWithLocations()
        {
            using var ctx = CreateInMemoryContext();

            var zone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "US Domestic",
                Description = "Shipping within US",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingZones.AddAsync(zone);

            var location = new ShippingZoneLocation
            {
                Id = Guid.NewGuid(),
                ShippingZoneId = zone.Id,
                CountryCode = "US",
                RegionCode = "CA",
                PostalCodePattern = "9*"
            };
            await ctx.ShippingZoneLocations.AddAsync(location);
            await ctx.SaveChangesAsync();

            var zones = await ctx.ShippingZones.Include(z => z.Locations).ToListAsync();
            Assert.Single(zones);
            var loc = zones[0].Locations.First();
            Assert.Equal("US", loc.CountryCode);
            Assert.Equal("CA", loc.RegionCode);
        }

        [Fact]
        public async Task ShippingMethod_CanBeCreatedWithRates()
        {
            using var ctx = CreateInMemoryContext();

            var zone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "US",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingZones.AddAsync(zone);

            var method = new ShippingMethod
            {
                Id = Guid.NewGuid(),
                ShippingZoneId = zone.Id,
                Name = "Standard Shipping",
                Type = "flat_rate",
                BaseRate = 9.99m,
                FreeShippingThreshold = 100m,
                EstimatedDaysMin = 3,
                EstimatedDaysMax = 5,
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingMethods.AddAsync(method);

            var rate = new ShippingRate
            {
                Id = Guid.NewGuid(),
                ShippingMethodId = method.Id,
                ConditionType = "weight",
                ConditionOperator = ">=",
                ConditionValueMin = 10,
                ConditionValueMax = 50,
                Rate = 14.99m,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingRates.AddAsync(rate);
            await ctx.SaveChangesAsync();

            var methods = await ctx.ShippingMethods.Include(m => m.Rates).ToListAsync();
            Assert.Single(methods);
            Assert.Equal("flat_rate", methods[0].Type);
            var rateEntity = methods[0].Rates.First();
            Assert.Equal(14.99m, rateEntity.Rate);
        }

        [Fact]
        public async Task TaxCategory_CanBeCreatedWithRates()
        {
            using var ctx = CreateInMemoryContext();

            var category = new TaxCategory
            {
                Id = Guid.NewGuid(),
                Name = "Standard Goods",
                Description = "Standard tax rate for physical goods",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.TaxCategories.AddAsync(category);

            var rate = new TaxRate
            {
                Id = Guid.NewGuid(),
                TaxCategoryId = category.Id,
                CountryCode = "US",
                RegionCode = "CA",
                PostalCodePattern = "9*",
                Rate = 0.0825m, // 8.25%
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.TaxRates.AddAsync(rate);
            await ctx.SaveChangesAsync();

            var categories = await ctx.TaxCategories.Include(c => c.Rates).ToListAsync();
            Assert.Single(categories);
            var rateEntity = categories[0].Rates.First();
            Assert.Equal(0.0825m, rateEntity.Rate);
            Assert.Equal("CA", rateEntity.RegionCode);
        }
    }
}