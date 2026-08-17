using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Queries.Products;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class ProductSearchTests
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

        private async Task SeedProducts(ApplicationDbContext ctx)
        {
            var categoryA = new Category { Id = Guid.NewGuid(), Name = "Electronics", IsActive = true };
            var categoryB = new Category { Id = Guid.NewGuid(), Name = "Books", IsActive = true };
            var brand = new Brand { Id = Guid.NewGuid(), Name = "Acme" };
            await ctx.Categories.AddRangeAsync(categoryA, categoryB);
            await ctx.Brands.AddAsync(brand);
            await ctx.Products.AddRangeAsync(
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Wireless Mouse",
                    Slug = "wireless-mouse",
                    Sku = "WM-001",
                    ShortDescription = "A sleek wireless mouse",
                    CategoryId = categoryA.Id,
                    BrandId = brand.Id,
                    BasePrice = 25m,
                    IsActive = true,
                    IsDeleted = false,
                    IsFeatured = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Mechanical Keyboard",
                    Slug = "mechanical-keyboard",
                    Sku = "MK-002",
                    ShortDescription = "Tactile mechanical keyboard",
                    CategoryId = categoryA.Id,
                    BrandId = brand.Id,
                    BasePrice = 120m,
                    IsActive = true,
                    IsDeleted = false,
                    IsFeatured = false,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-3)
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "C# Programming Guide",
                    Slug = "csharp-guide",
                    Sku = "BK-003",
                    ShortDescription = "A great book",
                    CategoryId = categoryB.Id,
                    BasePrice = 45m,
                    IsActive = true,
                    IsDeleted = false,
                    IsFeatured = false,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Retired Product",
                    Slug = "retired-product",
                    Sku = "RP-004",
                    CategoryId = categoryB.Id,
                    BasePrice = 10m,
                    IsActive = false,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task GetProducts_SearchTerm_FiltersByName()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());

            var result = await handler.Handle(new GetProductsQuery { SearchTerm = "mouse" });

            Assert.Single(result);
            Assert.Equal("Wireless Mouse", result[0].Name);
        }

        [Fact]
        public async Task GetProducts_SearchTerm_MatchesSku()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());

            var result = await handler.Handle(new GetProductsQuery { SearchTerm = "MK-002" });

            Assert.Single(result);
            Assert.Equal("Mechanical Keyboard", result[0].Name);
        }

        [Fact]
        public async Task GetProducts_CategoryFilter_ReturnsOnlyCategory()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());
            var electronicsId = await ctx.Categories.Where(c => c.Name == "Electronics").Select(c => c.Id).SingleAsync();

            var result = await handler.Handle(new GetProductsQuery { CategoryId = electronicsId });

            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Contains(new[] { "Wireless Mouse", "Mechanical Keyboard" }, n => n == p.Name));
        }

        [Fact]
        public async Task GetProducts_PriceRange_FiltersByBasePrice()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());

            var result = await handler.Handle(new GetProductsQuery { MinPrice = 40m, MaxPrice = 100m });

            Assert.Single(result);
            Assert.Equal("C# Programming Guide", result[0].Name);
        }

        [Fact]
        public async Task GetProducts_SortByPriceAsc_OrdersCorrectly()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());

            var result = await handler.Handle(new GetProductsQuery { SortBy = "price_asc" });

            Assert.Equal(4, result.Count);
            Assert.Equal(10m, result[0].BasePrice);
            Assert.Equal(120m, result[^1].BasePrice);
        }

        [Fact]
        public async Task GetProducts_IsActiveFalse_ReturnsInactive()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());

            var result = await handler.Handle(new GetProductsQuery { IsActive = false });

            Assert.Single(result);
            Assert.Equal("Retired Product", result[0].Name);
        }

        [Fact]
        public async Task GetProducts_SortByFeatured_OrdersFeaturedFirst()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());

            var result = await handler.Handle(new GetProductsQuery { SortBy = "featured" });

            Assert.Equal("Wireless Mouse", result[0].Name);
        }

        [Fact]
        public async Task GetProducts_DeletedProducts_AreExcluded()
        {
            using var ctx = CreateInMemoryContext();
            await SeedProducts(ctx);
            ctx.Products.Remove(await ctx.Products.FirstAsync(p => p.Name == "Wireless Mouse"));
            await ctx.SaveChangesAsync();
            var handler = new GetProductsQueryHandler(ctx, CreateMapper());

            var result = await handler.Handle(new GetProductsQuery());

            Assert.Equal(3, result.Count);
        }
    }
}