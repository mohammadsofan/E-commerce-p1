using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminShippingCommandHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IMapper CreateMapper()
        {
            return new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        [Fact]
        public async Task CreateShippingZone_CreatesZoneWithLocations()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateShippingZoneCommandHandler(ctx, CreateMapper());

            var command = new CreateShippingZoneCommand
            {
                Name = "US Domestic",
                Description = "Shipping within US",
                IsActive = true,
                Locations = new List<CreateShippingZoneLocationCommand>
                {
                    new CreateShippingZoneLocationCommand { CountryCode = "US", RegionCode = "CA" }
                }
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal("US Domestic", result.Name);
            Assert.True(result.IsActive);
            Assert.Single(result.Locations);
            Assert.Equal("US", result.Locations[0].CountryCode);
        }

        [Fact]
        public async Task UpdateShippingZone_UpdatesZoneAndLocations()
        {
            using var ctx = CreateInMemoryContext();

            var zone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "Old Zone",
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
                RegionCode = "CA"
            };
            await ctx.ShippingZoneLocations.AddAsync(location);
            await ctx.SaveChangesAsync();

            var handler = new UpdateShippingZoneCommandHandler(ctx, CreateMapper());
            var command = new UpdateShippingZoneCommand
            {
                Id = zone.Id,
                Name = "New Zone",
                IsActive = false,
                RowVersion = zone.RowVersion,
                Locations = new List<UpdateShippingZoneLocationCommand>
                {
                    new UpdateShippingZoneLocationCommand { Id = location.Id, CountryCode = "US", RegionCode = "NY" },
                    new UpdateShippingZoneLocationCommand { CountryCode = "CA" }
                }
            };

            var result = await handler.Handle(command);

            Assert.Equal("New Zone", result.Name);
            Assert.False(result.IsActive);
            Assert.Equal(2, result.Locations.Count);
        }

        [Fact]
        public async Task DeleteShippingZone_DeletesZoneAndChildren()
        {
            using var ctx = CreateInMemoryContext();

            var zone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "Zone",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingZones.AddAsync(zone);

            var method = new ShippingMethod
            {
                Id = Guid.NewGuid(),
                ShippingZoneId = zone.Id,
                Name = "Standard",
                Type = "flat_rate",
                BaseRate = 5,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingMethods.AddAsync(method);
            await ctx.SaveChangesAsync();

            var handler = new DeleteShippingZoneCommandHandler(ctx);
            await handler.Handle(new DeleteShippingZoneCommand { Id = zone.Id });

            Assert.Empty(await ctx.ShippingZones.ToListAsync());
            Assert.Empty(await ctx.ShippingMethods.ToListAsync());
        }

        [Fact]
        public async Task DeleteShippingZone_NotFound_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new DeleteShippingZoneCommandHandler(ctx);
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteShippingZoneCommand { Id = Guid.NewGuid() }));
        }

        [Fact]
        public async Task CreateShippingMethod_CreatesMethodWithRates()
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
            await ctx.SaveChangesAsync();

            var handler = new CreateShippingMethodCommandHandler(ctx, CreateMapper());
            var command = new CreateShippingMethodCommand
            {
                ShippingZoneId = zone.Id,
                Name = "Standard Shipping",
                Type = "flat_rate",
                BaseRate = 9.99m,
                FreeShippingThreshold = 100m,
                EstimatedDaysMin = 3,
                EstimatedDaysMax = 5,
                IsActive = true,
                DisplayOrder = 1,
                Rates = new List<CreateShippingRateCommand>
                {
                    new CreateShippingRateCommand { ConditionType = "weight", ConditionOperator = ">=", ConditionValueMin = 10, ConditionValueMax = 50, Rate = 14.99m }
                }
            };

            var result = await handler.Handle(command);

            Assert.Equal("Standard Shipping", result.Name);
            Assert.Equal("flat_rate", result.Type);
            Assert.Equal(9.99m, result.BaseRate);
            Assert.Single(result.Rates);
            Assert.Equal(14.99m, result.Rates[0].Rate);
        }

        [Fact]
        public async Task CreateShippingMethod_ZoneNotFound_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateShippingMethodCommandHandler(ctx, CreateMapper());
            var command = new CreateShippingMethodCommand { ShippingZoneId = Guid.NewGuid(), Name = "M", Type = "flat_rate" };
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task UpdateShippingMethod_UpdatesMethodAndRates()
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
                Name = "Old Method",
                Type = "flat_rate",
                BaseRate = 5,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingMethods.AddAsync(method);
            await ctx.SaveChangesAsync();

            var handler = new UpdateShippingMethodCommandHandler(ctx, CreateMapper());
            var command = new UpdateShippingMethodCommand
            {
                Id = method.Id,
                ShippingZoneId = zone.Id,
                Name = "Express",
                Type = "price_based",
                BaseRate = 19.99m,
                IsActive = true,
                RowVersion = method.RowVersion,
                Rates = new List<UpdateShippingRateCommand>
                {
                    new UpdateShippingRateCommand { ConditionType = "price", ConditionOperator = ">=", ConditionValueMin = 0, ConditionValueMax = 100, Rate = 19.99m }
                }
            };

            var result = await handler.Handle(command);

            Assert.Equal("Express", result.Name);
            Assert.Equal("price_based", result.Type);
            Assert.Equal(19.99m, result.BaseRate);
            Assert.Single(result.Rates);
        }

        [Fact]
        public async Task CreateShippingRateOnly_CreatesRate()
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
                Name = "Standard",
                Type = "flat_rate",
                BaseRate = 5,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ShippingMethods.AddAsync(method);
            await ctx.SaveChangesAsync();

            var handler = new CreateShippingRateOnlyCommandHandler(ctx, CreateMapper());
            var command = new CreateShippingRateOnlyCommand
            {
                ShippingMethodId = method.Id,
                ConditionType = "weight",
                ConditionOperator = ">=",
                ConditionValueMin = 0,
                ConditionValueMax = 10,
                Rate = 7.99m
            };

            var result = await handler.Handle(command);

            Assert.Equal(7.99m, result.Rate);
            Assert.Equal(method.Id, result.ShippingMethodId);
        }

        [Fact]
        public async Task GetShippingZones_ReturnsPagedResults()
        {
            using var ctx = CreateInMemoryContext();

            for (int i = 0; i < 3; i++)
            {
                await ctx.ShippingZones.AddAsync(new ShippingZone
                {
                    Id = Guid.NewGuid(),
                    Name = $"Zone {i}",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            await ctx.SaveChangesAsync();

            var handler = new GetAdminShippingZonesQueryHandler(ctx, CreateMapper());
            var query = new GetAdminShippingZonesQuery { Page = 1, PageSize = 10 };

            var result = await handler.Handle(query);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
        }
    }
}