using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateProductVariantCommandHandler : ICommandHandler<CreateProductVariantCommand, AdminProductVariantDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateProductVariantCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductVariantDto> Handle(CreateProductVariantCommand command, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FindAsync(new object[] { command.ProductId }, cancellationToken);
            if (product == null)
                throw new Domain.Exceptions.NotFoundException("Product", command.ProductId);

            var variant = new ProductVariant
            {
                ProductId = command.ProductId,
                Sku = command.Sku,
                Barcode = command.Barcode,
                Name = command.Name,
                Price = command.Price,
                CostPrice = command.CostPrice,
                CompareAtPrice = command.CompareAtPrice,
                Weight = command.Weight,
                Length = command.Length,
                Width = command.Width,
                Height = command.Height,
                IsActive = command.IsActive,
                TrackInventory = command.TrackInventory,
                AllowBackorder = command.AllowBackorder,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.ProductVariants.Add(variant);

            foreach (var imgCmd in command.Images)
            {
                var image = new ProductImage
                {
                    ProductId = command.ProductId,
                    ProductVariantId = variant.Id,
                    Url = imgCmd.Url,
                    AltText = imgCmd.AltText,
                    IsPrimary = imgCmd.IsPrimary,
                    SortOrder = imgCmd.SortOrder,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.ProductImages.Add(image);
            }

            foreach (var attrCmd in command.Attributes)
            {
                var variantAttr = new ProductVariantAttribute
                {
                    ProductVariantId = variant.Id,
                    ProductAttributeId = attrCmd.ProductAttributeId,
                    Value = attrCmd.Value,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _db.ProductVariantAttributes.Add(variantAttr);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetVariantDtoAsync(variant.Id, cancellationToken);
        }

        private async Task<AdminProductVariantDto> GetVariantDtoAsync(Guid variantId, CancellationToken cancellationToken)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.ProductAttribute)
                .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

            if (variant == null)
                throw new Domain.Exceptions.NotFoundException("ProductVariant", variantId);

            var dto = _mapper.Map<AdminProductVariantDto>(variant);
            dto.Images = variant.Images.Select(_mapper.Map<AdminProductImageDto>).ToList();
            dto.Attributes = variant.VariantAttributes.Select(va => new AdminProductVariantAttributeDto
            {
                Id = va.Id,
                ProductVariantId = va.ProductVariantId,
                ProductAttributeId = va.ProductAttributeId,
                AttributeName = va.ProductAttribute.Name,
                AttributeCode = va.ProductAttribute.Code,
                Value = va.Value,
                CreatedAt = va.CreatedAt,
                UpdatedAt = va.UpdatedAt
            }).ToList();

            return dto;
        }
    }

    public class UpdateProductVariantCommandHandler : ICommandHandler<UpdateProductVariantCommand, AdminProductVariantDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateProductVariantCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductVariantDto> Handle(UpdateProductVariantCommand command, CancellationToken cancellationToken = default)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.VariantAttributes)
                .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);

            if (variant == null)
                throw new Domain.Exceptions.NotFoundException("ProductVariant", command.Id);

            // Optimistic concurrency check
            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(variant);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            variant.Sku = command.Sku;
            variant.Barcode = command.Barcode;
            variant.Name = command.Name;
            variant.Price = command.Price;
            variant.CostPrice = command.CostPrice;
            variant.CompareAtPrice = command.CompareAtPrice;
            variant.Weight = command.Weight;
            variant.Length = command.Length;
            variant.Width = command.Width;
            variant.Height = command.Height;
            variant.IsActive = command.IsActive;
            variant.TrackInventory = command.TrackInventory;
            variant.AllowBackorder = command.AllowBackorder;
            variant.UpdatedAt = DateTimeOffset.UtcNow;

            // Handle images
            foreach (var imgCmd in command.Images)
            {
                if (imgCmd.IsDeleted && imgCmd.Id.HasValue)
                {
                    var img = variant.Images.FirstOrDefault(i => i.Id == imgCmd.Id.Value);
                    if (img != null)
                        _db.ProductImages.Remove(img);
                }
                else if (imgCmd.Id.HasValue)
                {
                    var img = variant.Images.FirstOrDefault(i => i.Id == imgCmd.Id.Value);
                    if (img != null)
                    {
                        img.Url = imgCmd.Url;
                        img.AltText = imgCmd.AltText;
                        img.IsPrimary = imgCmd.IsPrimary;
                        img.SortOrder = imgCmd.SortOrder;
                    }
                }
                else
                {
                    var img = new ProductImage
                    {
                        ProductId = variant.ProductId,
                        ProductVariantId = variant.Id,
                        Url = imgCmd.Url,
                        AltText = imgCmd.AltText,
                        IsPrimary = imgCmd.IsPrimary,
                        SortOrder = imgCmd.SortOrder,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _db.ProductImages.Add(img);
                }
            }

            // Handle attributes
            foreach (var attrCmd in command.Attributes)
            {
                if (attrCmd.IsDeleted && attrCmd.Id.HasValue)
                {
                    var attr = variant.VariantAttributes.FirstOrDefault(a => a.Id == attrCmd.Id.Value);
                    if (attr != null)
                        _db.ProductVariantAttributes.Remove(attr);
                }
                else if (attrCmd.Id.HasValue)
                {
                    var attr = variant.VariantAttributes.FirstOrDefault(a => a.Id == attrCmd.Id.Value);
                    if (attr != null)
                    {
                        attr.Value = attrCmd.Value;
                        attr.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
                else
                {
                    var attr = new ProductVariantAttribute
                    {
                        ProductVariantId = variant.Id,
                        ProductAttributeId = attrCmd.ProductAttributeId,
                        Value = attrCmd.Value,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    _db.ProductVariantAttributes.Add(attr);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetVariantDtoAsync(variant.Id, cancellationToken);
        }

        private async Task<AdminProductVariantDto> GetVariantDtoAsync(Guid variantId, CancellationToken cancellationToken)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.ProductAttribute)
                .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

            if (variant == null)
                throw new Domain.Exceptions.NotFoundException("ProductVariant", variantId);

            var dto = _mapper.Map<AdminProductVariantDto>(variant);
            dto.Images = variant.Images.Select(_mapper.Map<AdminProductImageDto>).ToList();
            dto.Attributes = variant.VariantAttributes.Select(va => new AdminProductVariantAttributeDto
            {
                Id = va.Id,
                ProductVariantId = va.ProductVariantId,
                ProductAttributeId = va.ProductAttributeId,
                AttributeName = va.ProductAttribute.Name,
                AttributeCode = va.ProductAttribute.Code,
                Value = va.Value,
                CreatedAt = va.CreatedAt,
                UpdatedAt = va.UpdatedAt
            }).ToList();

            return dto;
        }
    }

    public class DeleteProductVariantCommandHandler : ICommandHandler<DeleteProductVariantCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteProductVariantCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteProductVariantCommand command, CancellationToken cancellationToken = default)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.VariantAttributes)
                .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);

            if (variant == null)
                throw new Domain.Exceptions.NotFoundException("ProductVariant", command.Id);

            _db.ProductImages.RemoveRange(variant.Images);
            _db.ProductVariantAttributes.RemoveRange(variant.VariantAttributes);
            _db.ProductVariants.Remove(variant);

            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}