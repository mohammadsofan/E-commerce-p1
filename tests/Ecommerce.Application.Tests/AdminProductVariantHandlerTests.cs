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
    public class AdminProductVariantHandlerTests
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
        public async Task CreateProductVariant_CreatesVariantWithImagesAndAttributes()
        {
            using var ctx = CreateInMemoryContext();

            // Seed product
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Parent Product",
                Slug = "parent-product",
                Sku = "PARENT-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);
            await ctx.SaveChangesAsync();

            // Seed attribute
            var attribute = new ProductAttribute
            {
                Id = Guid.NewGuid(),
                Name = "Color",
                Code = "color",
                DisplayType = "color",
                IsVariant = true,
                IsFilterable = true,
                IsRequired = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductAttributes.AddAsync(attribute);
            await ctx.SaveChangesAsync();

            var handler = new CreateProductVariantCommandHandler(ctx, CreateMapper());

            var command = new CreateProductVariantCommand
            {
                ProductId = product.Id,
                Sku = "PARENT-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                CostPrice = 50m,
                IsActive = true,
                TrackInventory = true,
                AllowBackorder = false,
                Images = new List<CreateProductImageCommand>
                {
                    new CreateProductImageCommand { Url = "https://example.com/red.jpg", AltText = "Red variant", IsPrimary = true, SortOrder = 0 }
                },
                Attributes = new List<CreateProductVariantAttributeCommand>
                {
                    new CreateProductVariantAttributeCommand { ProductAttributeId = attribute.Id, Value = "Red" }
                }
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal(product.Id, result.ProductId);
            Assert.Equal("PARENT-001-RED", result.Sku);
            Assert.Equal("Red Variant", result.Name);
            Assert.Equal(99.99m, result.Price);
            Assert.Single(result.Images);
            Assert.Single(result.Attributes);
            Assert.Equal("Color", result.Attributes[0].AttributeName);
            Assert.Equal("Red", result.Attributes[0].Value);
        }

        [Fact]
        public async Task CreateProductVariant_InvalidProductId_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateProductVariantCommandHandler(ctx, CreateMapper());

            var command = new CreateProductVariantCommand
            {
                ProductId = Guid.NewGuid(), // Non-existent product
                Sku = "INVALID-001",
                Name = "Invalid Variant",
                Price = 10m
            };

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task UpdateProductVariant_UpdatesFieldsAndManagesImagesAttributes()
        {
            using var ctx = CreateInMemoryContext();

            // Seed product
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Parent Product",
                Slug = "parent-product",
                Sku = "PARENT-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            // Seed attribute
            var attribute = new ProductAttribute
            {
                Id = Guid.NewGuid(),
                Name = "Size",
                Code = "size",
                DisplayType = "text",
                IsVariant = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductAttributes.AddAsync(attribute);

            // Seed variant
            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Sku = "PARENT-001-S",
                Name = "Small",
                Price = 50m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductVariants.AddAsync(variant);

            // Seed existing image
            var existingImage = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductVariantId = variant.Id,
                Url = "https://example.com/old.jpg",
                AltText = "Old image",
                IsPrimary = true,
                SortOrder = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductImages.AddAsync(existingImage);

            await ctx.SaveChangesAsync();

            var handler = new UpdateProductVariantCommandHandler(ctx, CreateMapper());

            var command = new UpdateProductVariantCommand
            {
                Id = variant.Id,
                Sku = "PARENT-001-S-UPD",
                Name = "Small Updated",
                Price = 55m,
                IsActive = true,
                RowVersion = variant.RowVersion,
                Images = new List<UpdateProductImageCommand>
                {
                    new UpdateProductImageCommand { Id = existingImage.Id, Url = "https://example.com/new.jpg", AltText = "New image", IsPrimary = true, SortOrder = 0 },
                    new UpdateProductImageCommand { Url = "https://example.com/additional.jpg", AltText = "Additional", IsPrimary = false, SortOrder = 1 }
                },
                Attributes = new List<UpdateProductVariantAttributeCommand>
                {
                    new UpdateProductVariantAttributeCommand { ProductAttributeId = attribute.Id, Value = "Small" }
                }
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal("PARENT-001-S-UPD", result.Sku);
            Assert.Equal("Small Updated", result.Name);
            Assert.Equal(55m, result.Price);
            Assert.Equal(2, result.Images.Count);
            Assert.Single(result.Attributes);
        }

        [Fact]
        public async Task DeleteProductVariant_RemovesVariantAndRelatedData()
        {
            using var ctx = CreateInMemoryContext();

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Parent",
                Slug = "parent",
                Sku = "PAR",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Sku = "PAR-V1",
                Name = "Variant 1",
                Price = 50m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductVariants.AddAsync(variant);

            var image = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductVariantId = variant.Id,
                Url = "https://example.com/img.jpg",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductImages.AddAsync(image);

            var attr = new ProductAttribute { Id = Guid.NewGuid(), Name = "Test", Code = "test", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            await ctx.ProductAttributes.AddAsync(attr);

            var variantAttr = new ProductVariantAttribute
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                ProductAttributeId = attr.Id,
                Value = "Test Value",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductVariantAttributes.AddAsync(variantAttr);

            await ctx.SaveChangesAsync();

            var handler = new DeleteProductVariantCommandHandler(ctx);
            await handler.Handle(new DeleteProductVariantCommand { Id = variant.Id });

            var deletedVariant = await ctx.ProductVariants.FindAsync(variant.Id);
            Assert.Null(deletedVariant);

            var deletedImages = await ctx.ProductImages.Where(i => i.ProductVariantId == variant.Id).ToListAsync();
            Assert.Empty(deletedImages);

            var deletedAttrs = await ctx.ProductVariantAttributes.Where(a => a.ProductVariantId == variant.Id).ToListAsync();
            Assert.Empty(deletedAttrs);
        }

        [Fact]
        public async Task GetAdminProductVariants_ReturnsPagedFilteredResults()
        {
            using var ctx = CreateInMemoryContext();

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Parent",
                Slug = "parent",
                Sku = "PAR",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            var variants = new List<ProductVariant>
            {
                new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Sku = "PAR-RED", Name = "Red", Price = 50m, IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Sku = "PAR-BLUE", Name = "Blue", Price = 60m, IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Sku = "PAR-GREEN", Name = "Green", Price = 70m, IsActive = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
            };
            await ctx.ProductVariants.AddRangeAsync(variants);
            await ctx.SaveChangesAsync();

            var queryHandler = new GetAdminProductVariantsQueryHandler(ctx, CreateMapper());

            var query = new GetAdminProductVariantsQuery { ProductId = product.Id, Page = 1, PageSize = 10 };
            var result = await queryHandler.Handle(query);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
        }

        [Fact]
        public async Task GetAdminProductVariants_FiltersByIsActive()
        {
            using var ctx = CreateInMemoryContext();

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Parent",
                Slug = "parent",
                Sku = "PAR",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            var variants = new List<ProductVariant>
            {
                new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Sku = "PAR-1", Name = "Active", Price = 50m, IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Sku = "PAR-2", Name = "Inactive", Price = 60m, IsActive = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
            };
            await ctx.ProductVariants.AddRangeAsync(variants);
            await ctx.SaveChangesAsync();

            var queryHandler = new GetAdminProductVariantsQueryHandler(ctx, CreateMapper());

            var query = new GetAdminProductVariantsQuery { IsActive = true };
            var result = await queryHandler.Handle(query);

            Assert.Single(result.Items);
            Assert.True(result.Items[0].IsActive);
        }
    }
}