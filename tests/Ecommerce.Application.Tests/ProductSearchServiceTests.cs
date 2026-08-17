using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class ProductSearchServiceTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private async Task<Guid> SeedProduct(ApplicationDbContext ctx, string name, string sku, string description, bool active = true)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = name.ToLowerInvariant().Replace(" ", "-"),
                Sku = sku,
                ShortDescription = description,
                BasePrice = 20m,
                IsActive = active,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            return product.Id;
        }

        [Fact]
        public async Task Search_IndexedProducts_ReturnsRelevanceRankedHits()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var mouseId = await SeedProduct(ctx, "Wireless Mouse", "WM-001", "A wireless mouse for productivity");
            await service.IndexProductAsync(mouseId);
            await SeedProduct(ctx, "Gaming Keyboard", "GK-002", "RGB mechanical keyboard");
            var gamingKeyboardId = await ctx.Products.SingleAsync(p => p.Sku == "GK-002");
            await service.IndexProductAsync(gamingKeyboardId.Id);

            var result = await service.SearchAsync("wireless");

            Assert.Single(result.Items);
            Assert.Equal(mouseId, result.Items[0].ProductId);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task Search_EmptyTerm_ReturnsNoResults()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var id = await SeedProduct(ctx, "Mouse", "M-001", "");
            await service.IndexProductAsync(id);

            var result = await service.SearchAsync("  ");

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task Search_ExactNameMatch_RanksFirst()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var exactId = await SeedProduct(ctx, "Keyboard", "K-001", "nothing");
            var containsId = await SeedProduct(ctx, "Mechanical Keyboard Pro", "K-002", "nothing");
            await service.IndexProductAsync(exactId);
            await service.IndexProductAsync(containsId);

            var result = await service.SearchAsync("keyboard");

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(exactId, result.Items[0].ProductId);
            Assert.True(result.Items[0].Score > result.Items[1].Score);
        }

        [Fact]
        public async Task Search_MatchOnSku_ReturnsProduct()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var id = await SeedProduct(ctx, "Widget", "WID-99", "generic");
            await service.IndexProductAsync(id);

            var result = await service.SearchAsync("WID-99");

            Assert.Single(result.Items);
            Assert.Equal(id, result.Items[0].ProductId);
        }

        [Fact]
        public async Task Search_InactiveProduct_IsExcluded()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var activeId = await SeedProduct(ctx, "Active Item", "A-001", "");
            var inactiveId = await SeedProduct(ctx, "Inactive Item", "A-002", "", active: false);
            await service.IndexProductAsync(activeId);
            await service.IndexProductAsync(inactiveId);

            var result = await service.SearchAsync("item");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(activeId, result.Items[0].ProductId);
        }

        [Fact]
        public async Task RemoveFromIndex_DeletesDocument()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var id = await SeedProduct(ctx, "Mouse", "M-001", "");
            await service.IndexProductAsync(id);
            await service.RemoveFromIndexAsync(id);

            var result = await service.SearchAsync("mouse");

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task RebuildIndex_RecreatesAllDocuments()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var id1 = await SeedProduct(ctx, "Alpha Product", "A-1", "");
            var id2 = await SeedProduct(ctx, "Beta Product", "B-1", "");
            await service.IndexProductAsync(id1);

            await service.RebuildIndexAsync();

            var result = await service.SearchAsync("product");
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, await ctx.ProductSearchDocuments.CountAsync());
        }

        [Fact]
        public async Task IndexProduct_UpdatesExistingDocument()
        {
            using var ctx = CreateContext();
            var service = new ProductSearchService(ctx);

            var id = await SeedProduct(ctx, "Old Name", "O-001", "old description");
            await service.IndexProductAsync(id);

            var product = await ctx.Products.SingleAsync(p => p.Id == id);
            product.Name = "New Name";
            await ctx.SaveChangesAsync();

            await service.IndexProductAsync(id);

            var result = await service.SearchAsync("new name");
            Assert.Single(result.Items);
            Assert.Equal(id, result.Items[0].ProductId);

            var noResult = await service.SearchAsync("old name");
            Assert.Empty(noResult.Items);
        }

        [Fact]
        public async Task CreateProductHandler_WithSearchService_IndexesProduct()
        {
            using var ctx = CreateContext();
            var searchService = new ProductSearchService(ctx);
            var handler = new CreateProductCommandHandler(
                ctx,
                new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfileForTests>()).CreateMapper(),
                searchService);

            var dto = await handler.Handle(new CreateProductCommand
            {
                Name = "Searchable Widget",
                Slug = "searchable-widget",
                Sku = "SW-1",
                IsActive = true
            });

            var result = await searchService.SearchAsync("searchable");
            Assert.Single(result.Items);
            Assert.Equal(dto.Id, result.Items[0].ProductId);
        }

        [Fact]
        public async Task DeleteProductHandler_HardDelete_RemovesFromIndex()
        {
            using var ctx = CreateContext();
            var searchService = new ProductSearchService(ctx);
            var createHandler = new CreateProductCommandHandler(
                ctx,
                new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfileForTests>()).CreateMapper(),
                searchService);

            var dto = await createHandler.Handle(new CreateProductCommand
            {
                Name = "Doomed Widget",
                Slug = "doomed-widget",
                Sku = "DW-1",
                IsActive = true
            });

            var deleteHandler = new DeleteProductCommandHandler(ctx, searchService);
            await deleteHandler.Handle(new DeleteProductCommand { Id = dto.Id, HardDelete = true });

            var result = await searchService.SearchAsync("doomed");
            Assert.Empty(result.Items);
            Assert.Equal(0, await ctx.ProductSearchDocuments.CountAsync());
        }
    }
}