using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Products;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class ProductFeatureSortingTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfileForTests>());
            return config.CreateMapper();
        }

        [Fact]
        public async Task GetProducts_WithSortByFeatured_ReturnsOnlyFeaturedProducts()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateMapper();

            var category = new Category { Id = Guid.NewGuid(), Name = "Clothing", Slug = "clothing", CreatedAt = DateTime.UtcNow };
            db.Categories.Add(category);

            db.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = "Featured Shirt",
                Slug = "featured-shirt",
                Sku = "SKU-1",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = 30m,
                CurrencyCode = "USD",
                IsActive = true,
                IsFeatured = true,
                CategoryId = category.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            db.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = "Regular Pants",
                Slug = "regular-pants",
                Sku = "SKU-2",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = 50m,
                CurrencyCode = "USD",
                IsActive = true,
                IsFeatured = false,
                CategoryId = category.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();

            var handler = new GetProductsQueryHandler(db, mapper);
            var query = new GetProductsQuery { SortBy = "featured" };

            // Act
            var result = await handler.Handle(query);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("Featured Shirt", result.Items.First().Name);
        }

        [Fact]
        public async Task CreateProduct_AssignsCategoryIdCorrectly()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateMapper();

            var category = new Category { Id = Guid.NewGuid(), Name = "Shoes", Slug = "shoes", CreatedAt = DateTime.UtcNow };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var handler = new CreateProductCommandHandler(db, mapper);
            var command = new CreateProductCommand
            {
                Name = "Running Shoes",
                Slug = "running-shoes",
                Sku = "SHOES-001",
                ShortDescription = "Fast shoes",
                Description = "Full description",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = 120m,
                CurrencyCode = "USD",
                CategoryId = category.Id,
                IsActive = true
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(category.Id, result.CategoryId);

            var inDb = await db.Products.FindAsync(result.Id);
            Assert.NotNull(inDb);
            Assert.Equal(category.Id, inDb.CategoryId);
        }

        [Fact]
        public async Task UpdateProduct_UpdatesCategoryIdCorrectly()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateMapper();

            var oldCat = new Category { Id = Guid.NewGuid(), Name = "Old Cat", Slug = "old-cat", CreatedAt = DateTime.UtcNow };
            var newCat = new Category { Id = Guid.NewGuid(), Name = "New Cat", Slug = "new-cat", CreatedAt = DateTime.UtcNow };
            db.Categories.AddRange(oldCat, newCat);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product X",
                Slug = "product-x",
                Sku = "SKU-X",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = 100m,
                CurrencyCode = "USD",
                IsActive = true,
                CategoryId = oldCat.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var handler = new UpdateProductCommandHandler(db, mapper);
            var command = new UpdateProductCommand
            {
                Id = product.Id,
                Name = "Product X Updated",
                Slug = "product-x",
                Sku = "SKU-X",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = 110m,
                CurrencyCode = "USD",
                CategoryId = newCat.Id,
                IsActive = true
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.Equal(newCat.Id, result.CategoryId);
            var inDb = await db.Products.FindAsync(product.Id);
            Assert.NotNull(inDb);
            Assert.Equal(newCat.Id, inDb.CategoryId);
        }
    }
}

