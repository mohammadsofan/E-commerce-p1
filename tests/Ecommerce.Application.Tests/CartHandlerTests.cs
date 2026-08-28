using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Carts;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Mappings;
using Ecommerce.Application.Queries.Carts;
using Ecommerce.Application.Queries.Products;
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
            public bool IsAdmin => false;

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
                IsActive = true,
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

        [Fact]
        public async Task AddToCart_ExceedsAvailableStock_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var warehouseId = Guid.NewGuid();
            var inv = new InventoryItem(product.Id, warehouseId, 5);
            context.InventoryItems.Add(inv);
            await context.SaveChangesAsync();

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            // Requesting 6 when only 5 is available
            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 6 }));
        }

        [Fact]
        public async Task AddToCart_Variant_ExceedsVariantStock_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "Blue Variant",
                Sku = "SKU-BLUE",
                Price = 15m,
                IsActive = true
            };
            context.ProductVariants.Add(variant);

            var warehouseId = Guid.NewGuid();
            // Variant stock is 2
            var inv = new InventoryItem(product.Id, warehouseId, quantityOnHand: 2, productVariantId: variant.Id);
            context.InventoryItems.Add(inv);
            await context.SaveChangesAsync();

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            // Requesting 3 of the variant when only 2 is available
            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new AddToCartCommand
                {
                    ProductId = product.Id,
                    ProductVariantId = variant.Id,
                    Quantity = 3
                }));
        }

        [Fact]
        public async Task AddToCart_Variant_WithinStock_Succeeds()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "Red Variant",
                Sku = "SKU-RED",
                Price = 25m,
                IsActive = true
            };
            context.ProductVariants.Add(variant);

            var warehouseId = Guid.NewGuid();
            var inv = new InventoryItem(product.Id, warehouseId, quantityOnHand: 10, productVariantId: variant.Id);
            context.InventoryItems.Add(inv);
            await context.SaveChangesAsync();

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            var result = await handler.Handle(new AddToCartCommand
            {
                ProductId = product.Id,
                ProductVariantId = variant.Id,
                Quantity = 5
            });

            Assert.Single(result.Items);
            Assert.Equal(5, result.Items.First().Quantity);
            Assert.Equal(25m, result.Items.First().UnitPrice);
            Assert.Equal(125m, result.TotalAmount);
        }

        [Fact]
        public async Task AddToCart_VariantRequiredButNull_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "Red Variant",
                Sku = "SKU-RED",
                Price = 25m,
                IsActive = true
            };
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            // Null variant ID
            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new AddToCartCommand
                {
                    ProductId = product.Id,
                    ProductVariantId = null,
                    Quantity = 1
                }));
        }

        [Fact]
        public async Task AddToCart_VariantRequiredButEmpty_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "Red Variant",
                Sku = "SKU-RED",
                Price = 25m,
                IsActive = true
            };
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            // Empty variant ID
            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new AddToCartCommand
                {
                    ProductId = product.Id,
                    ProductVariantId = Guid.Empty,
                    Quantity = 1
                }));
        }

        [Fact]
        public async Task AddToCart_VariantBelongsToOtherProduct_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product1 = await SeedProductAsync(context);
            var product2 = await SeedProductAsync(context);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product2.Id, // Belongs to product 2
                Name = "Red Variant",
                Sku = "SKU-RED",
                Price = 25m,
                IsActive = true
            };
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new AddToCartCommand
                {
                    ProductId = product1.Id, // Adding product 1
                    ProductVariantId = variant.Id, // With variant from product 2
                    Quantity = 1
                }));
        }

        [Fact]
        public async Task AddToCart_UnknownVariant_ThrowsNotFoundException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var handler = new AddToCartCommandHandler(context, currentUser, mapper);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new AddToCartCommand
                {
                    ProductId = product.Id,
                    ProductVariantId = Guid.NewGuid(), // Nonexistent
                    Quantity = 1
                }));
        }

        [Fact]
        public async Task UpdateCartItem_ExceedsAvailableStock_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new FakeCurrentUserService(Guid.NewGuid());
            var product = await SeedProductAsync(context);

            var warehouseId = Guid.NewGuid();
            var inv = new InventoryItem(product.Id, warehouseId, 5);
            context.InventoryItems.Add(inv);
            await context.SaveChangesAsync();

            var addHandler = new AddToCartCommandHandler(context, currentUser, mapper);
            var added = await addHandler.Handle(new AddToCartCommand { ProductId = product.Id, Quantity = 2 });
            var itemId = added.Items.First().Id;

            var updateHandler = new UpdateCartItemCommandHandler(context, currentUser, mapper);

            // Updating from 2 to 10 when only 5 is available
            await Assert.ThrowsAsync<DomainException>(() =>
                updateHandler.Handle(new UpdateCartItemCommand
                {
                    CartItemId = itemId,
                    Quantity = 10
                }));
        }
    }

    public class ProductVariantQueryTests
    {
        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            return config.CreateMapper();
        }

        [Fact]
        public async Task GetProductById_IncludesVariants_WithAttributesAndStock()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "T-Shirt",
                Slug = "t-shirt",
                Sku = "TSHIRT",
                BasePrice = 20m,
                IsActive = true
            };
            context.Products.Add(product);

            var attrColor = new ProductAttribute { Id = Guid.NewGuid(), Name = "Color", Code = "color" };
            var attrSize = new ProductAttribute { Id = Guid.NewGuid(), Name = "Size", Code = "size" };
            context.ProductAttributes.AddRange(attrColor, attrSize);

            var variantRedM = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Name = "T-Shirt Red M",
                Sku = "TSHIRT-RED-M",
                Price = 22m,
                IsActive = true
            };
            var variantBlueL = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Name = "T-Shirt Blue L",
                Sku = "TSHIRT-BLU-L",
                Price = 25m,
                IsActive = true
            };
            context.ProductVariants.AddRange(variantRedM, variantBlueL);

            var va1 = new ProductVariantAttribute { Id = Guid.NewGuid(), ProductVariantId = variantRedM.Id, ProductAttributeId = attrColor.Id, ProductAttribute = attrColor, Value = "Red" };
            var va2 = new ProductVariantAttribute { Id = Guid.NewGuid(), ProductVariantId = variantRedM.Id, ProductAttributeId = attrSize.Id, ProductAttribute = attrSize, Value = "M" };
            var va3 = new ProductVariantAttribute { Id = Guid.NewGuid(), ProductVariantId = variantBlueL.Id, ProductAttributeId = attrColor.Id, ProductAttribute = attrColor, Value = "Blue" };
            var va4 = new ProductVariantAttribute { Id = Guid.NewGuid(), ProductVariantId = variantBlueL.Id, ProductAttributeId = attrSize.Id, ProductAttribute = attrSize, Value = "L" };
            context.ProductVariantAttributes.AddRange(va1, va2, va3, va4);

            var warehouseId = Guid.NewGuid();
            var invRed = new InventoryItem(productId, warehouseId, quantityOnHand: 8, productVariantId: variantRedM.Id);
            var invBlue = new InventoryItem(productId, warehouseId, quantityOnHand: 3, productVariantId: variantBlueL.Id);
            context.InventoryItems.AddRange(invRed, invBlue);

            await context.SaveChangesAsync();

            var handler = new GetProductByIdQueryHandler(context, mapper);
            var result = await handler.Handle(new GetProductByIdQuery { Id = productId });

            Assert.NotNull(result);
            Assert.Equal("T-Shirt", result.Name);
            Assert.Equal(2, result.Variants.Count);

            var redDto = result.Variants.First(v => v.Id == variantRedM.Id);
            Assert.Equal("TSHIRT-RED-M", redDto.Sku);
            Assert.Equal(22m, redDto.Price);
            Assert.Equal(8, redDto.AvailableStock);
            Assert.Equal(2, redDto.Attributes.Count);
            Assert.Contains(redDto.Attributes, a => a.AttributeName == "Color" && a.Value == "Red");
            Assert.Contains(redDto.Attributes, a => a.AttributeName == "Size" && a.Value == "M");

            var blueDto = result.Variants.First(v => v.Id == variantBlueL.Id);
            Assert.Equal("TSHIRT-BLU-L", blueDto.Sku);
            Assert.Equal(25m, blueDto.Price);
            Assert.Equal(3, blueDto.AvailableStock);
            Assert.Contains(blueDto.Attributes, a => a.AttributeName == "Color" && a.Value == "Blue");
            Assert.Contains(blueDto.Attributes, a => a.AttributeName == "Size" && a.Value == "L");
        }

        [Fact]
        public async Task GetProductBySlug_IncludesVariants_WithAttributesAndStock()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Sneakers",
                Slug = "running-sneakers",
                Sku = "SNK",
                BasePrice = 100m,
                IsActive = true
            };
            context.Products.Add(product);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Name = "Sneakers 42",
                Sku = "SNK-42",
                Price = 110m,
                IsActive = true
            };
            context.ProductVariants.Add(variant);

            var warehouseId = Guid.NewGuid();
            var inv = new InventoryItem(productId, warehouseId, quantityOnHand: 15, productVariantId: variant.Id);
            context.InventoryItems.Add(inv);

            await context.SaveChangesAsync();

            var handler = new GetProductBySlugQueryHandler(context, mapper);
            var result = await handler.Handle(new GetProductBySlugQuery { Slug = "running-sneakers" });

            Assert.NotNull(result);
            Assert.Equal("running-sneakers", result.Slug);
            Assert.Single(result.Variants);
            Assert.Equal(15, result.Variants.First().AvailableStock);
        }
    }
}



