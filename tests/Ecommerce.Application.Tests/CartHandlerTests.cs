using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Carts;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Mappings;
using Ecommerce.Application.Queries.Carts;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class CartHandlerTests
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
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            return config.CreateMapper();
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId { get; }
            public string UserName => "test-user";

            public FakeCurrentUserService(Guid userId) => UserId = userId;
        }

        private static async Task<Product> SeedProductAsync(ApplicationDbContext context, decimal price = 10m, string slug = "test-product")
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = slug,
                Slug = slug,
                Sku = $"SKU-{slug}",
                ShortDescription = "desc",
                Description = "desc",
                ProductType = "Physical",
                Status = "Active",
                BasePrice = price,
                CurrencyCode = "USD",
                SeoTitle = "title",
                SeoDescription = "desc",
                SeoKeywords = "kw",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                RowVersion = Array.Empty<byte>()
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();
            return product;
        }

        [Fact]
        public async Task AddToCart_CreatesCartAndAddsItem()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);
            var result = await handler.Handle(new AddToCartCommand
            {
                ProductId = product.Id,
                Quantity = 2
            });

            Assert.Equal("Active", result.Status);
            Assert.Single(result.Items);
            Assert.Equal(2, result.Items.First().Quantity);
            Assert.Equal(product.BasePrice, result.Items.First().UnitPrice);
            Assert.Equal(20m, result.TotalAmount);
        }

        [Fact]
        public async Task AddToCart_UnknownProduct_ThrowsNotFound()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new AddToCartCommand { ProductId = Guid.NewGuid(), Quantity = 1 }));
        }

        [Fact]
        public async Task AddToCart_MergesQuantityForSameProduct()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            await handler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 1 });
            var result = await handler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 2 });

            Assert.Single(result.Items);
            Assert.Equal(3, result.Items.First().Quantity);
            Assert.Equal(30m, result.TotalAmount);
        }

        [Fact]
        public async Task GetCart_ReturnsExistingCartWithItems()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var addHandler = new AddToCartCommandHandler(context, currentUser, mapper);
            var added = await addHandler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 1 });

            var getHandler = new GetCartQueryHandler(context, currentUser, mapper);
            var result = await getHandler.Handle(new GetCartQuery());

            Assert.Equal(added.Id, result.Id);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task UpdateCartItem_UpdatesQuantity()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var addHandler = new AddToCartCommandHandler(context, currentUser, mapper);
            var added = await addHandler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 2 });
            var itemId = added.Items.First().Id;

            var updateHandler = new UpdateCartItemCommandHandler(context, currentUser, mapper);
            var result = await updateHandler.Handle(new UpdateCartItemCommand
            {
                CartItemId = itemId,
                Quantity = 5
            });

            Assert.Equal(5, result.Items.First().Quantity);
            Assert.Equal(50m, result.TotalAmount);
        }

        [Fact]
        public async Task UpdateCartItem_ZeroQuantity_RemovesItem()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var addHandler = new AddToCartCommandHandler(context, currentUser, mapper);
            var added = await addHandler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 2 });
            var itemId = added.Items.First().Id;

            var updateHandler = new UpdateCartItemCommandHandler(context, currentUser, mapper);
            var result = await updateHandler.Handle(new UpdateCartItemCommand
            {
                CartItemId = itemId,
                Quantity = 0
            });

            Assert.Empty(result.Items);
            Assert.Equal(0m, result.TotalAmount);
        }

        [Fact]
        public async Task RemoveFromCart_RemovesItem()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var addHandler = new AddToCartCommandHandler(context, currentUser, mapper);
            var added = await addHandler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 1 });
            var itemId = added.Items.First().Id;

            var removeHandler = new RemoveFromCartCommandHandler(context, currentUser, mapper);
            var result = await removeHandler.Handle(new RemoveFromCartCommand { CartItemId = itemId });

            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task ClearCart_RemovesAllItems()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());

            // Seed a cart with two items directly to test the Clear handler in isolation.
            var cart = Cart.Create(currentUser.UserId, null);
            cart.AddItem(Guid.NewGuid(), null, "Item A", 10m, 1);
            cart.AddItem(Guid.NewGuid(), null, "Item B", 5m, 2);
            context.Carts.Add(cart);
            await context.SaveChangesAsync();

            var clearHandler = new ClearCartCommandHandler(context, currentUser, mapper);
            var result = await clearHandler.Handle(new ClearCartCommand());

            Assert.Empty(result.Items);
            Assert.Equal(0m, result.TotalAmount);
        }

        [Fact]
        public async Task RemoveFromCart_NoCart_ThrowsNotFound()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());

            var removeHandler = new RemoveFromCartCommandHandler(context, currentUser, mapper);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                removeHandler.Handle(new RemoveFromCartCommand { CartItemId = Guid.NewGuid() }));
        }
    }
}
