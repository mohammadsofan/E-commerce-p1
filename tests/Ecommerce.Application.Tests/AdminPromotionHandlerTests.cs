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
    public class AdminPromotionHandlerTests
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
        public async Task CreatePromotion_CreatesPromotion()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreatePromotionCommandHandler(ctx, CreateMapper());

            var command = new CreatePromotionCommand
            {
                Name = "Summer Sale",
                Description = "20% off all items",
                Type = "percentage_discount",
                RulesJson = "{\"percentage\": 20}",
                IsActive = true,
                Priority = 10,
                AllowCombine = false,
                UsageLimit = 1000
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal("Summer Sale", result.Name);
            Assert.Equal("percentage_discount", result.Type);
            Assert.Equal(10, result.Priority);
        }

        [Fact]
        public async Task UpdatePromotion_UpdatesFields()
        {
            using var ctx = CreateInMemoryContext();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Old Promo",
                Type = "buy_x_get_y",
                RulesJson = "{}",
                IsActive = true,
                Priority = 5,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Promotions.AddAsync(promo);
            await ctx.SaveChangesAsync();

            var handler = new UpdatePromotionCommandHandler(ctx, CreateMapper());
            var command = new UpdatePromotionCommand
            {
                Id = promo.Id,
                Name = "Updated Promo",
                Type = "tiered_discount",
                RulesJson = "{\"tiers\": []}",
                IsActive = false,
                Priority = 20,
                RowVersion = promo.RowVersion
            };

            var result = await handler.Handle(command);

            Assert.Equal("Updated Promo", result.Name);
            Assert.Equal("tiered_discount", result.Type);
            Assert.False(result.IsActive);
            Assert.Equal(20, result.Priority);
        }

        [Fact]
        public async Task DeletePromotion_UsedPromotion_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var promo = new Promotion
            {
                Id = Guid.NewGuid(),
                Name = "Used Promo",
                Type = "percentage_discount",
                RulesJson = "{}",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Promotions.AddAsync(promo);

            var usage = new PromotionUsage
            {
                Id = Guid.NewGuid(),
                PromotionId = promo.Id,
                UserId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                DiscountAmount = 10,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.PromotionUsages.AddAsync(usage);
            await ctx.SaveChangesAsync();

            var handler = new DeletePromotionCommandHandler(ctx);
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new DeletePromotionCommand { Id = promo.Id }));
        }
    }
}

