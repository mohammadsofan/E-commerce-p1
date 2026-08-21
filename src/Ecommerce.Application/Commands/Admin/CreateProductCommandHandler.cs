using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, AdminProductDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IProductSearchService? _searchService;

        public CreateProductCommandHandler(IApplicationDbContext db, IMapper mapper, IProductSearchService? searchService = null)
        {
            _db = db;
            _mapper = mapper;
            _searchService = searchService;
        }

        public async Task<AdminProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken = default)
        {
            // Check if slug is unique
            var existingSlug = await _db.Products.AnyAsync(p => p.Slug == command.Slug, cancellationToken);
            if (existingSlug)
                throw new DomainException($"Product with slug '{command.Slug}' already exists.");

            // Check if SKU is unique
            var existingSku = await _db.Products.AnyAsync(p => p.Sku == command.Sku, cancellationToken);
            if (existingSku)
                throw new DomainException($"Product with SKU '{command.Sku}' already exists.");

            // Find default warehouse if not specified
            Guid warehouseId = command.WarehouseId ?? Guid.Empty;
            if (warehouseId == Guid.Empty)
            {
                var defaultWarehouse = await _db.Warehouses
                    .Where(w => w.IsActive)
                    .OrderBy(w => w.Name)
                    .FirstOrDefaultAsync(cancellationToken);
                if (defaultWarehouse != null)
                    warehouseId = defaultWarehouse.Id;
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                BrandId = command.BrandId,
                CategoryId = command.CategoryId,
                Name = command.Name,
                Slug = command.Slug,
                Sku = command.Sku,
                ShortDescription = command.ShortDescription,
                Description = command.Description,
                ProductType = command.ProductType,
                Status = command.Status,
                BasePrice = command.BasePrice,
                CostPrice = command.CostPrice,
                CompareAtPrice = command.CompareAtPrice,
                CurrencyCode = command.CurrencyCode,
                Weight = command.Weight,
                Length = command.Length,
                Width = command.Width,
                Height = command.Height,
                IsActive = command.IsActive,
                IsFeatured = command.IsFeatured,
                IsDigital = command.IsDigital,
                RequiresShipping = command.RequiresShipping,
                TrackInventory = command.TrackInventory,
                AllowBackorder = command.AllowBackorder,
                SeoTitle = command.SeoTitle,
                SeoDescription = command.SeoDescription,
                SeoKeywords = command.SeoKeywords,
                AttributesJson = command.Attributes != null && command.Attributes.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(command.Attributes) : null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _db.Products.AddAsync(product, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            // Create inventory item for the product if TrackInventory is true
            if (command.TrackInventory)
            {
                int initialStock = command.Stock ?? 0;
                var inventoryItem = new InventoryItem(product.Id, warehouseId, initialStock)
                {
                    AllowBackorder = command.AllowBackorder
                };

                _db.InventoryItems.Add(inventoryItem);
                await _db.SaveChangesAsync(cancellationToken);
            }

            if (_searchService != null)
                await _searchService.IndexProductAsync(product.Id, cancellationToken);

            return _mapper.Map<AdminProductDto>(product);
        }
    }
}