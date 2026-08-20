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
    public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, AdminProductDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IProductSearchService? _searchService;

        public UpdateProductCommandHandler(IApplicationDbContext db, IMapper mapper, IProductSearchService? searchService = null)
        {
            _db = db;
            _mapper = mapper;
            _searchService = searchService;
        }

        public async Task<AdminProductDto> Handle(UpdateProductCommand command, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (product == null)
                throw new NotFoundException("Product", command.Id);

            // Check if slug is unique (excluding current product)
            var existingSlug = await _db.Products
                .AnyAsync(p => p.Slug == command.Slug && p.Id != command.Id, cancellationToken);
            if (existingSlug)
                throw new DomainException($"Product with slug '{command.Slug}' already exists.");

            // Check if SKU is unique (excluding current product)
            var existingSku = await _db.Products
                .AnyAsync(p => p.Sku == command.Sku && p.Id != command.Id, cancellationToken);
            if (existingSku)
                throw new DomainException($"Product with SKU '{command.Sku}' already exists.");

            product.BrandId = command.BrandId;
            product.CategoryId = command.CategoryId;
            product.Name = command.Name;
            product.Slug = command.Slug;
            product.Sku = command.Sku;
            product.ShortDescription = command.ShortDescription;
            product.Description = command.Description;
            product.ProductType = command.ProductType;
            product.Status = command.Status;
            product.BasePrice = command.BasePrice;
            product.CostPrice = command.CostPrice;
            product.CompareAtPrice = command.CompareAtPrice;
            product.CurrencyCode = command.CurrencyCode;
            product.TaxCategoryId = command.TaxCategoryId;
            product.Weight = command.Weight;
            product.Length = command.Length;
            product.Width = command.Width;
            product.Height = command.Height;
            product.IsActive = command.IsActive;
            product.IsFeatured = command.IsFeatured;
            product.IsDigital = command.IsDigital;
            product.RequiresShipping = command.RequiresShipping;
            product.TrackInventory = command.TrackInventory;
            product.AllowBackorder = command.AllowBackorder;
            product.SeoTitle = command.SeoTitle;
            product.SeoDescription = command.SeoDescription;
            product.SeoKeywords = command.SeoKeywords;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            // Handle stock update if provided
            if (command.Stock.HasValue)
            {
                var inventoryItem = await _db.InventoryItems
                    .FirstOrDefaultAsync(i => i.ProductId == product.Id && i.ProductVariantId == null, cancellationToken);
                if (inventoryItem != null)
                {
                    inventoryItem.SetStock(command.Stock.Value);
                }
                else if (command.TrackInventory)
                {
                    // Create inventory item if it doesn't exist and track inventory is true
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

                    var newInventoryItem = new InventoryItem(product.Id, warehouseId, command.Stock.Value)
                    {
                        AllowBackorder = command.AllowBackorder
                    };

                    _db.InventoryItems.Add(newInventoryItem);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (_searchService != null)
                await _searchService.IndexProductAsync(product.Id, cancellationToken);

            return _mapper.Map<AdminProductDto>(product);
        }
    }
}