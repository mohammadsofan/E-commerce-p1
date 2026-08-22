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
        public async Task GetActiveShippingMethods_ReturnsOnlyActiveMethodsAndZones()
        {
            using var ctx = CreateInMemoryContext();

            var activeZone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "الضفة الغربية",
                Description = "مدن الضفة الغربية",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var inactiveZone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "منطقة غير مفعلة",
                Description = "وصف",
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            await ctx.ShippingZones.AddRangeAsync(activeZone, inactiveZone);

            var method1 = new ShippingMethod
            {
                Id = Guid.NewGuid(),
                ShippingZoneId = activeZone.Id,
                Name = "توصيل الضفة الغربية",
                Type = "flat_rate",
                BaseRate = 5.50m,
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var methodInactive = new ShippingMethod
            {
                Id = Guid.NewGuid(),
                ShippingZoneId = activeZone.Id,
                Name = "شحن معطل",
                Type = "flat_rate",
                BaseRate = 10m,
                IsActive = false,
                DisplayOrder = 2,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var methodInInactiveZone = new ShippingMethod
            {
                Id = Guid.NewGuid(),
                ShippingZoneId = inactiveZone.Id,
                Name = "شحن في منطقة معطلة",
                Type = "flat_rate",
                BaseRate = 12m,
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            await ctx.ShippingMethods.AddRangeAsync(method1, methodInactive, methodInInactiveZone);
            await ctx.SaveChangesAsync();

            var handler = new Ecommerce.Application.Queries.Shipping.GetActiveShippingMethodsQueryHandler(ctx);
            var result = await handler.Handle(new Ecommerce.Application.Queries.Shipping.GetActiveShippingMethodsQuery());

            Assert.Single(result);
            Assert.Equal("توصيل الضفة الغربية", result[0].Name);
            Assert.Equal("الضفة الغربية", result[0].ZoneName);
            Assert.Equal(5.50m, result[0].BaseRate);
        }
    }
}

