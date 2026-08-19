using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateProductImageCommandHandler : ICommandHandler<CreateProductImageCommand, AdminProductImageDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateProductImageCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductImageDto> Handle(CreateProductImageCommand command, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FindAsync(new object[] { command.ProductId }, cancellationToken);
            if (product == null)
                throw new DomainException("Product not found");

            if (command.ProductVariantId.HasValue)
            {
                var variant = await _db.ProductVariants.FindAsync(new object[] { command.ProductVariantId.Value }, cancellationToken);
                if (variant == null || variant.ProductId != command.ProductId)
                    throw new DomainException("Product variant not found or does not belong to product");
            }

            // If this is primary, unset other primary images for the same product/variant
            if (command.IsPrimary)
            {
                var existingPrimary = await _db.ProductImages
                    .Where(i => i.ProductId == command.ProductId && i.ProductVariantId == command.ProductVariantId && i.IsPrimary)
                    .ToListAsync(cancellationToken);

                foreach (var img in existingPrimary)
                    img.IsPrimary = false;
            }

            var image = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = command.ProductId,
                ProductVariantId = command.ProductVariantId,
                Url = command.Url,
                AltText = command.AltText,
                IsPrimary = command.IsPrimary,
                SortOrder = command.SortOrder,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ProductImages.Add(image);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminProductImageDto>(image);
        }
    }

    public class UpdateProductImageCommandHandler : ICommandHandler<UpdateProductImageCommand, AdminProductImageDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateProductImageCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductImageDto> Handle(UpdateProductImageCommand command, CancellationToken cancellationToken = default)
        {
            var image = await _db.ProductImages
                .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
            if (image == null)
                throw new DomainException("Product image not found");

            if (image.ProductId != command.ProductId)
                throw new DomainException("Image does not belong to the specified product");

            // If this is primary, unset other primary images for the same product/variant
            if (command.IsPrimary)
            {
                var existingPrimary = await _db.ProductImages
                    .Where(i => i.ProductId == command.ProductId && i.ProductVariantId == command.ProductVariantId && i.IsPrimary && i.Id != command.Id)
                    .ToListAsync(cancellationToken);

                foreach (var img in existingPrimary)
                    img.IsPrimary = false;
            }

            image.Url = command.Url;
            image.AltText = command.AltText;
            image.IsPrimary = command.IsPrimary;
            image.SortOrder = command.SortOrder;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminProductImageDto>(image);
        }
    }

    public class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteProductImageCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteProductImageCommand command, CancellationToken cancellationToken = default)
        {
            var image = await _db.ProductImages
                .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
            if (image == null)
                throw new DomainException("Product image not found");

            _db.ProductImages.Remove(image);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}