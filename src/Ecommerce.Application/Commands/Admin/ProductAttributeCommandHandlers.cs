using System;
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
    public class CreateProductAttributeCommandHandler : ICommandHandler<CreateProductAttributeCommand, AdminProductAttributeDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateProductAttributeCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductAttributeDto> Handle(CreateProductAttributeCommand command, CancellationToken cancellationToken = default)
        {
            var attribute = new ProductAttribute
            {
                Name = command.Name,
                Code = command.Code,
                DisplayType = command.DisplayType,
                IsFilterable = command.IsFilterable,
                IsVariant = command.IsVariant,
                IsRequired = command.IsRequired,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.ProductAttributes.Add(attribute);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminProductAttributeDto>(attribute);
        }
    }

    public class UpdateProductAttributeCommandHandler : ICommandHandler<UpdateProductAttributeCommand, AdminProductAttributeDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateProductAttributeCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductAttributeDto> Handle(UpdateProductAttributeCommand command, CancellationToken cancellationToken = default)
        {
            var attribute = await _db.ProductAttributes.FindAsync(new object[] { command.Id }, cancellationToken);

            if (attribute == null)
                throw new Domain.Exceptions.NotFoundException("ProductAttribute", command.Id);

            // Optimistic concurrency check
            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(attribute);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            attribute.Name = command.Name;
            attribute.Code = command.Code;
            attribute.DisplayType = command.DisplayType;
            attribute.IsFilterable = command.IsFilterable;
            attribute.IsVariant = command.IsVariant;
            attribute.IsRequired = command.IsRequired;
            attribute.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminProductAttributeDto>(attribute);
        }
    }

    public class DeleteProductAttributeCommandHandler : ICommandHandler<DeleteProductAttributeCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteProductAttributeCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteProductAttributeCommand command, CancellationToken cancellationToken = default)
        {
            var attribute = await _db.ProductAttributes.FindAsync(new object[] { command.Id }, cancellationToken);

            if (attribute == null)
                throw new Domain.Exceptions.NotFoundException("ProductAttribute", command.Id);

            // Check if attribute is used by any variants
            var usedByVariants = await _db.ProductVariantAttributes
                .AnyAsync(va => va.ProductAttributeId == command.Id, cancellationToken);

            if (usedByVariants)
                throw new Domain.Exceptions.DomainException("Cannot delete attribute that is used by product variants");

            _db.ProductAttributes.Remove(attribute);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}