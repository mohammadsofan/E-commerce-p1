using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Wishlist;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Wishlist;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class WishlistHandlerTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId { get; }
            public string UserName => "test-user";
            public bool IsAdmin => false;

            public FakeCurrentUserService(Guid userId) => UserId = userId;
        }

        private static async Task<Product> SeedProductAsync(ApplicationDbContext context, string name = "Test Product")
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = name.ToLower().Replace(" ", "-"),
                Sku = $"SKU-{Guid.NewGuid():N}",
                ShortDescription = "Short desc",
                Description = "Full desc",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = 99.99m,
                CurrencyCode = "USD",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();
            return product;
        }

        [Fact]
        public async Task AddToWishlist_WhenProductExists_AddsItemSuccessfully()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService(userId);
            var product = await SeedProductAsync(db, "Smart Watch");

            var handler = new AddToWishlistCommandHandler(db, currentUser);
            var command = new AddToWishlistCommand { ProductId = product.Id };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(product.Id, result.ProductId);
            Assert.Equal("Smart Watch", result.ProductName);
            Assert.Equal(99.99m, result.ProductPrice);

            var inDb = await db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == product.Id);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task AddToWishlist_WhenItemAlreadyExists_DoesNotDuplicate()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService(userId);
            var product = await SeedProductAsync(db, "Headphones");

            var handler = new AddToWishlistCommandHandler(db, currentUser);
            var command = new AddToWishlistCommand { ProductId = product.Id };

            // Act
            await handler.Handle(command);
            var secondResult = await handler.Handle(command);

            // Assert
            Assert.NotNull(secondResult);
            var count = await db.WishlistItems.CountAsync(w => w.UserId == userId && w.ProductId == product.Id);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task AddToWishlist_WhenProductNotFound_ThrowsNotFoundException()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService(userId);

            var handler = new AddToWishlistCommandHandler(db, currentUser);
            var command = new AddToWishlistCommand { ProductId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task RemoveFromWishlist_WhenItemExists_RemovesSuccessfully()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService(userId);
            var product = await SeedProductAsync(db, "Laptop");

            db.WishlistItems.Add(new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var handler = new RemoveFromWishlistCommandHandler(db, currentUser);
            var command = new RemoveFromWishlistCommand { ProductId = product.Id };

            // Act
            await handler.Handle(command);

            // Assert
            var inDb = await db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == product.Id);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task ClearWishlist_ClearsOnlyCurrentUserItems()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var currentUser1 = new FakeCurrentUserService(user1);

            var product1 = await SeedProductAsync(db, "Product 1");
            var product2 = await SeedProductAsync(db, "Product 2");

            db.WishlistItems.Add(new WishlistItem { Id = Guid.NewGuid(), UserId = user1, ProductId = product1.Id, CreatedAt = DateTime.UtcNow });
            db.WishlistItems.Add(new WishlistItem { Id = Guid.NewGuid(), UserId = user1, ProductId = product2.Id, CreatedAt = DateTime.UtcNow });
            db.WishlistItems.Add(new WishlistItem { Id = Guid.NewGuid(), UserId = user2, ProductId = product1.Id, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var handler = new ClearWishlistCommandHandler(db, currentUser1);
            var command = new ClearWishlistCommand();

            // Act
            await handler.Handle(command);

            // Assert
            var user1Items = await db.WishlistItems.Where(w => w.UserId == user1).ToListAsync();
            var user2Items = await db.WishlistItems.Where(w => w.UserId == user2).ToListAsync();

            Assert.Empty(user1Items);
            Assert.Single(user2Items);
        }

        [Fact]
        public async Task GetWishlist_ReturnsItemsWithProductDetails()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService(userId);

            var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics", CreatedAt = DateTime.UtcNow };
            var brand = new Brand { Id = Guid.NewGuid(), Name = "Apple", Slug = "apple", CreatedAt = DateTime.UtcNow };
            db.Categories.Add(category);
            db.Brands.Add(brand);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "iPhone 15",
                Slug = "iphone-15",
                Sku = "IPHONE-15",
                ShortDescription = "Latest iPhone",
                Description = "Description",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = 999m,
                CurrencyCode = "USD",
                IsActive = true,
                CategoryId = category.Id,
                BrandId = brand.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Products.Add(product);

            db.WishlistItems.Add(new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var handler = new GetWishlistQueryHandler(db, currentUser);
            var query = new GetWishlistQuery();

            // Act
            var result = await handler.Handle(query);

            // Assert
            Assert.Single(result);
            var item = result.First();
            Assert.Equal(product.Id, item.ProductId);
            Assert.Equal("iPhone 15", item.ProductName);
            Assert.Equal("iphone-15", item.ProductSlug);
            Assert.Equal(999m, item.ProductPrice);
            Assert.Equal("Electronics", item.CategoryName);
            Assert.Equal("Apple", item.BrandName);
        }

        [Fact]
        public async Task GetWishlist_IncludesVariantInventoryItems_CalculatesStockAccurately()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService(userId);
            var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Main Warehouse", Code = "WH-01" };
            db.Warehouses.Add(warehouse);

            var product = await SeedProductAsync(db, "Running Shoes");

            var variant1 = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "Size 42 - Black",
                Sku = "SHOE-42-BLK",
                Price = 120m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var variant2 = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "Size 43 - Black",
                Sku = "SHOE-43-BLK",
                Price = 120m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.ProductVariants.AddRange(variant1, variant2);

            var inv1 = new InventoryItem(product.Id, warehouse.Id, 8, variant1.Id);
            var inv2 = new InventoryItem(product.Id, warehouse.Id, 12, variant2.Id);
            db.InventoryItems.AddRange(inv1, inv2);

            db.WishlistItems.Add(new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var handler = new GetWishlistQueryHandler(db, currentUser);
            var query = new GetWishlistQuery();

            // Act
            var result = await handler.Handle(query);

            // Assert
            Assert.Single(result);
            var item = result.First();
            Assert.Equal(product.Id, item.ProductId);
            Assert.Equal(20, item.AvailableStock);
        }

        [Fact]
        public async Task AddToWishlist_WithVariantStock_ReturnsCorrectAvailableStock()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService(userId);
            var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Main Warehouse", Code = "WH-01" };
            db.Warehouses.Add(warehouse);

            var product = await SeedProductAsync(db, "T-Shirt");

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "Size L - Blue",
                Sku = "TSHIRT-L-BLU",
                Price = 30m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.ProductVariants.Add(variant);

            var inv = new InventoryItem(product.Id, warehouse.Id, 15, variant.Id);
            db.InventoryItems.Add(inv);
            await db.SaveChangesAsync();

            var handler = new AddToWishlistCommandHandler(db, currentUser);
            var command = new AddToWishlistCommand { ProductId = product.Id };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result.AvailableStock);
        }
    }
}

