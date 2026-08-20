using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Products;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class ProductRecommendationHandlerTests
    {
        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static IMapper CreateTestMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        [Fact]
        public async Task Handle_ReturnsFrequentlyBoughtTogetherProducts_RankedByFrequency()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();
            var handler = new GetFrequentlyBoughtTogetherQueryHandler(db, mapper);

            var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics" };
            await db.Categories.AddAsync(category);

            var prodA = new Product { Id = Guid.NewGuid(), Name = "Phone A", Slug = "phone-a", BasePrice = 999m, IsActive = true, CategoryId = category.Id };
            var prodB = new Product { Id = Guid.NewGuid(), Name = "Case B", Slug = "case-b", BasePrice = 29m, IsActive = true, CategoryId = category.Id };
            var prodC = new Product { Id = Guid.NewGuid(), Name = "Charger C", Slug = "charger-c", BasePrice = 19m, IsActive = true, CategoryId = category.Id };
            var prodD = new Product { Id = Guid.NewGuid(), Name = "Earphones D", Slug = "earphones-d", BasePrice = 49m, IsActive = true, CategoryId = category.Id };

            await db.Products.AddRangeAsync(prodA, prodB, prodC, prodD);

            // Order 1: A + B + C
            var order1 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001" };
            order1.AddItem(prodA.Id, Guid.NewGuid(), prodA.Name, 999m, 1);
            order1.AddItem(prodB.Id, Guid.NewGuid(), prodB.Name, 29m, 1);
            order1.AddItem(prodC.Id, Guid.NewGuid(), prodC.Name, 19m, 1);

            // Order 2: A + B
            var order2 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-002" };
            order2.AddItem(prodA.Id, Guid.NewGuid(), prodA.Name, 999m, 1);
            order2.AddItem(prodB.Id, Guid.NewGuid(), prodB.Name, 29m, 1);

            // Order 3: A + B
            var order3 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-003" };
            order3.AddItem(prodA.Id, Guid.NewGuid(), prodA.Name, 999m, 1);
            order3.AddItem(prodB.Id, Guid.NewGuid(), prodB.Name, 29m, 1);

            await db.Orders.AddRangeAsync(order1, order2, order3);
            await db.SaveChangesAsync();

            // Act: query recommendations for Phone A
            var query = new GetFrequentlyBoughtTogetherQuery(new List<Guid> { prodA.Id }, limit: 4);
            var result = await handler.Handle(query);

            // Assert: Case B appeared 3 times with A, Charger C appeared 1 time
            Assert.NotEmpty(result);
            Assert.Equal(prodB.Id, result[0].Id);
            Assert.Equal(prodC.Id, result[1].Id);
        }

        [Fact]
        public async Task Handle_ExcludesInputProductsFromRecommendations()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();
            var handler = new GetFrequentlyBoughtTogetherQueryHandler(db, mapper);

            var prodA = new Product { Id = Guid.NewGuid(), Name = "A", Slug = "a", BasePrice = 100m, IsActive = true };
            var prodB = new Product { Id = Guid.NewGuid(), Name = "B", Slug = "b", BasePrice = 200m, IsActive = true };
            var prodC = new Product { Id = Guid.NewGuid(), Name = "C", Slug = "c", BasePrice = 300m, IsActive = true };
            await db.Products.AddRangeAsync(prodA, prodB, prodC);

            var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001" };
            order.AddItem(prodA.Id, Guid.NewGuid(), prodA.Name, 100m, 1);
            order.AddItem(prodB.Id, Guid.NewGuid(), prodB.Name, 200m, 1);
            order.AddItem(prodC.Id, Guid.NewGuid(), prodC.Name, 300m, 1);

            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // Act: both A and B are in the cart
            var query = new GetFrequentlyBoughtTogetherQuery(new List<Guid> { prodA.Id, prodB.Id }, limit: 4);
            var result = await handler.Handle(query);

            // Assert: only C should be recommended, A and B must not be in the output
            Assert.Single(result);
            Assert.Equal(prodC.Id, result[0].Id);
        }

        [Fact]
        public async Task Handle_FallsBackToSameCategory_WhenNoOrderHistoryExists()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();
            var handler = new GetFrequentlyBoughtTogetherQueryHandler(db, mapper);

            var cat = new Category { Id = Guid.NewGuid(), Name = "Shoes", Slug = "shoes" };
            await db.Categories.AddAsync(cat);

            var prodA = new Product { Id = Guid.NewGuid(), Name = "Shoe A", Slug = "shoe-a", BasePrice = 100m, IsActive = true, CategoryId = cat.Id };
            var prodB = new Product { Id = Guid.NewGuid(), Name = "Shoe B", Slug = "shoe-b", BasePrice = 120m, IsActive = true, CategoryId = cat.Id };
            var prodOther = new Product { Id = Guid.NewGuid(), Name = "Unrelated", Slug = "unrelated", BasePrice = 50m, IsActive = true };

            await db.Products.AddRangeAsync(prodA, prodB, prodOther);
            await db.SaveChangesAsync();

            // Act: query recommendations for Shoe A (no order history)
            var query = new GetFrequentlyBoughtTogetherQuery(new List<Guid> { prodA.Id }, limit: 2);
            var result = await handler.Handle(query);

            // Assert: Shoe B should be recommended because it shares the same category
            Assert.NotEmpty(result);
            Assert.Equal(prodB.Id, result[0].Id);
        }

        [Fact]
        public async Task Handle_RespectsLimitParameter()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();
            var handler = new GetFrequentlyBoughtTogetherQueryHandler(db, mapper);

            var cat = new Category { Id = Guid.NewGuid(), Name = "General", Slug = "general" };
            await db.Categories.AddAsync(cat);

            var p1 = new Product { Id = Guid.NewGuid(), Name = "P1", Slug = "p1", BasePrice = 10m, IsActive = true, CategoryId = cat.Id };
            var p2 = new Product { Id = Guid.NewGuid(), Name = "P2", Slug = "p2", BasePrice = 20m, IsActive = true, CategoryId = cat.Id };
            var p3 = new Product { Id = Guid.NewGuid(), Name = "P3", Slug = "p3", BasePrice = 30m, IsActive = true, CategoryId = cat.Id };
            var p4 = new Product { Id = Guid.NewGuid(), Name = "P4", Slug = "p4", BasePrice = 40m, IsActive = true, CategoryId = cat.Id };

            await db.Products.AddRangeAsync(p1, p2, p3, p4);
            await db.SaveChangesAsync();

            // Act: limit to 2
            var query = new GetFrequentlyBoughtTogetherQuery(new List<Guid> { p1.Id }, limit: 2);
            var result = await handler.Handle(query);

            // Assert
            Assert.Equal(2, result.Count);
        }
    }
}
