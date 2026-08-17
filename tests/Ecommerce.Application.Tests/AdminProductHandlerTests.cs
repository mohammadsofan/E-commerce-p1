using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminProductHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateProduct_CreatesProduct_AndReturnsDto()
        {
            using var ctx = CreateInMemoryContext();

            var handler = new CreateProductCommandHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TEST-001",
                BasePrice = 29.99m,
                CostPrice = 15.00m,
                Status = "Active",
                IsActive = true
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal("Test Product", result.Name);
            Assert.Equal("test-product", result.Slug);
            Assert.Equal("TEST-001", result.Sku);
            Assert.Equal(29.99m, result.BasePrice);
            Assert.Equal("Active", result.Status);
            Assert.True(result.IsActive);

            var productInDb = await ctx.Products.FindAsync(result.Id);
            Assert.NotNull(productInDb);
            Assert.Equal("Test Product", productInDb.Name);
        }

        [Fact]
        public async Task CreateProduct_DuplicateSlug_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var handler = new CreateProductCommandHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var command1 = new CreateProductCommand
            {
                Name = "Product 1",
                Slug = "duplicate-slug",
                Sku = "SKU-001",
                BasePrice = 10m,
                Status = "Active"
            };

            await handler.Handle(command1);

            var command2 = new CreateProductCommand
            {
                Name = "Product 2",
                Slug = "duplicate-slug", // Same slug
                Sku = "SKU-002",
                BasePrice = 20m,
                Status = "Active"
            };

            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command2));
        }

        [Fact]
        public async Task CreateProduct_DuplicateSku_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var handler = new CreateProductCommandHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var command1 = new CreateProductCommand
            {
                Name = "Product 1",
                Slug = "slug-1",
                Sku = "DUPLICATE-SKU",
                BasePrice = 10m,
                Status = "Active"
            };

            await handler.Handle(command1);

            var command2 = new CreateProductCommand
            {
                Name = "Product 2",
                Slug = "slug-2",
                Sku = "DUPLICATE-SKU", // Same SKU
                BasePrice = 20m,
                Status = "Active"
            };

            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command2));
        }

        [Fact]
        public async Task UpdateProduct_UpdatesProduct_AndReturnsDto()
        {
            using var ctx = CreateInMemoryContext();

            // Seed product
            var product = new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Original Name",
                Slug = "original-slug",
                Sku = "SKU-001",
                BasePrice = 10m,
                Status = "Draft",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);
            await ctx.SaveChangesAsync();

            var handler = new UpdateProductCommandHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var command = new UpdateProductCommand
            {
                Id = product.Id,
                Name = "Updated Name",
                Slug = "updated-slug",
                Sku = "SKU-001",
                BasePrice = 25m,
                Status = "Active",
                IsActive = true
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("updated-slug", result.Slug);
            Assert.Equal(25m, result.BasePrice);
            Assert.Equal("Active", result.Status);

            var productInDb = await ctx.Products.FindAsync(product.Id);
            Assert.Equal("Updated Name", productInDb.Name);
            Assert.Equal("updated-slug", productInDb.Slug);
            Assert.Equal(25m, productInDb.BasePrice);
        }

        [Fact]
        public async Task UpdateProduct_NotFound_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var handler = new UpdateProductCommandHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var command = new UpdateProductCommand
            {
                Id = Guid.NewGuid(),
                Name = "Updated",
                Slug = "updated",
                Sku = "SKU-001",
                BasePrice = 10m,
                Status = "Active"
            };

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task DeleteProduct_SoftDeletes_ByDefault()
        {
            using var ctx = CreateInMemoryContext();

            var product = new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "To Delete",
                Slug = "to-delete",
                Sku = "DEL-001",
                BasePrice = 10m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);
            await ctx.SaveChangesAsync();

            var handler = new DeleteProductCommandHandler(ctx);

            var command = new DeleteProductCommand { Id = product.Id };
            await handler.Handle(command);

            var deleted = await ctx.Products.FindAsync(product.Id);
            Assert.NotNull(deleted);
            Assert.True(deleted.IsDeleted);
        }

        [Fact]
        public async Task DeleteProduct_HardDelete_RemovesFromDb()
        {
            using var ctx = CreateInMemoryContext();

            var product = new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Hard Delete",
                Slug = "hard-delete",
                Sku = "HD-001",
                BasePrice = 10m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);
            await ctx.SaveChangesAsync();

            var handler = new DeleteProductCommandHandler(ctx);

            var command = new DeleteProductCommand { Id = product.Id, HardDelete = true };
            await handler.Handle(command);

            var deleted = await ctx.Products.FindAsync(product.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task GetAdminProducts_ReturnsPagedResults()
        {
            using var ctx = CreateInMemoryContext();

            // Seed products
            var products = new List<Ecommerce.Domain.Entities.Product>
            {
                new Ecommerce.Domain.Entities.Product { Id = Guid.NewGuid(), Name = "Product A", Slug = "a", Sku = "A", BasePrice = 10m, Status = "Active", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Ecommerce.Domain.Entities.Product { Id = Guid.NewGuid(), Name = "Product B", Slug = "b", Sku = "B", BasePrice = 20m, Status = "Active", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Ecommerce.Domain.Entities.Product { Id = Guid.NewGuid(), Name = "Product C", Slug = "c", Sku = "C", BasePrice = 30m, Status = "Draft", IsActive = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            };
            await ctx.Products.AddRangeAsync(products);
            await ctx.SaveChangesAsync();

            var queryHandler = new GetAdminProductsQueryHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var query = new GetAdminProductsQuery { Page = 1, PageSize = 10 };
            var result = await queryHandler.Handle(query);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
        }

        [Fact]
        public async Task GetAdminProducts_FiltersByStatus()
        {
            using var ctx = CreateInMemoryContext();

            var products = new List<Ecommerce.Domain.Entities.Product>
            {
                new Ecommerce.Domain.Entities.Product { Id = Guid.NewGuid(), Name = "Active Product", Slug = "active", Sku = "ACT", BasePrice = 10m, Status = "Active", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Ecommerce.Domain.Entities.Product { Id = Guid.NewGuid(), Name = "Draft Product", Slug = "draft", Sku = "DRF", BasePrice = 20m, Status = "Draft", IsActive = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            };
            await ctx.Products.AddRangeAsync(products);
            await ctx.SaveChangesAsync();

            var queryHandler = new GetAdminProductsQueryHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var query = new GetAdminProductsQuery { Page = 1, PageSize = 10, Status = "Active" };
            var result = await queryHandler.Handle(query);

            Assert.Single(result.Items);
            Assert.Equal("Active", result.Items[0].Status);
        }

        [Fact]
        public async Task GetAdminProductById_ReturnsProduct()
        {
            using var ctx = CreateInMemoryContext();

            var product = new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TP-001",
                BasePrice = 49.99m,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);
            await ctx.SaveChangesAsync();

            var queryHandler = new GetAdminProductByIdQueryHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var query = new GetAdminProductByIdQuery { Id = product.Id };
            var result = await queryHandler.Handle(query);

            Assert.NotNull(result);
            Assert.Equal("Test Product", result.Name);
            Assert.Equal(49.99m, result.BasePrice);
        }

        [Fact]
        public async Task GetAdminProductById_NotFound_Throws()
        {
            using var ctx = CreateInMemoryContext();

            var queryHandler = new GetAdminProductByIdQueryHandler(ctx, new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper());

            var query = new GetAdminProductByIdQuery { Id = Guid.NewGuid() };

            await Assert.ThrowsAsync<NotFoundException>(() => queryHandler.Handle(query));
        }
    }

    public class AutoMapperProfileForTests : AutoMapper.Profile
    {
        public AutoMapperProfileForTests()
        {
            CreateMap<Ecommerce.Domain.Entities.Product, Ecommerce.Application.DTOs.AdminProductDto>();
            CreateMap<Ecommerce.Domain.Entities.Product, Ecommerce.Application.DTOs.ProductDto>();
            CreateMap<Ecommerce.Domain.Entities.Currency, Ecommerce.Application.DTOs.CurrencyDto>();
            CreateMap<Ecommerce.Domain.Entities.ExchangeRate, Ecommerce.Application.DTOs.ExchangeRateDto>()
                .ForMember(d => d.FromCurrencyCode, opt => opt.Ignore())
                .ForMember(d => d.ToCurrencyCode, opt => opt.Ignore());
            CreateMap<Ecommerce.Domain.Entities.ProductVariant, Ecommerce.Application.DTOs.AdminProductVariantDto>();
            CreateMap<Ecommerce.Domain.Entities.ProductImage, Ecommerce.Application.DTOs.AdminProductImageDto>();
            CreateMap<Ecommerce.Domain.Entities.Coupon, Ecommerce.Application.DTOs.AdminCouponDto>();
            CreateMap<Ecommerce.Domain.Entities.Promotion, Ecommerce.Application.DTOs.AdminPromotionDto>();
            CreateMap<Ecommerce.Domain.Entities.ShippingZone, Ecommerce.Application.DTOs.AdminShippingZoneDto>();
            CreateMap<Ecommerce.Domain.Entities.ShippingZoneLocation, Ecommerce.Application.DTOs.AdminShippingZoneLocationDto>();
            CreateMap<Ecommerce.Domain.Entities.ShippingMethod, Ecommerce.Application.DTOs.AdminShippingMethodDto>();
            CreateMap<Ecommerce.Domain.Entities.ShippingRate, Ecommerce.Application.DTOs.AdminShippingRateDto>();
            CreateMap<Ecommerce.Domain.Entities.TaxCategory, Ecommerce.Application.DTOs.AdminTaxCategoryDto>();
            CreateMap<Ecommerce.Domain.Entities.TaxRate, Ecommerce.Application.DTOs.AdminTaxRateDto>();
            CreateMap<Ecommerce.Domain.Entities.Notification, Ecommerce.Application.DTOs.AdminNotificationDto>();
            CreateMap<Ecommerce.Domain.Entities.NotificationTemplate, Ecommerce.Application.DTOs.AdminNotificationTemplateDto>();
            CreateMap<Ecommerce.Domain.Entities.NotificationPreference, Ecommerce.Application.DTOs.AdminNotificationPreferenceDto>();
            CreateMap<Ecommerce.Domain.Entities.NotificationChannel, Ecommerce.Application.DTOs.AdminNotificationChannelDto>();
        }
    }
}