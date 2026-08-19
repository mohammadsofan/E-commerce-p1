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
    public class CreateInventoryCommandHandler : ICommandHandler<CreateInventoryCommand, AdminInventoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateInventoryCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminInventoryDto> Handle(CreateInventoryCommand command, CancellationToken cancellationToken = default)
        {
            // Verify product exists
            var product = await _db.Products.FindAsync(new object[] { command.ProductId }, cancellationToken);
            if (product == null)
                throw new NotFoundException("Product", command.ProductId);

            // Verify variant if provided
            if (command.ProductVariantId.HasValue)
            {
                var variant = await _db.ProductVariants.FindAsync(new object[] { command.ProductVariantId.Value }, cancellationToken);
                if (variant == null || variant.ProductId != command.ProductId)
                    throw new DomainException("Product variant not found or does not belong to product");
            }

            // Verify warehouse exists
            var warehouse = await _db.Warehouses.FindAsync(new object[] { command.WarehouseId }, cancellationToken);
            if (warehouse == null)
                throw new NotFoundException("Warehouse", command.WarehouseId);

            // Check if inventory item already exists for this product/variant/warehouse combination
            var existing = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == command.ProductId 
                    && i.ProductVariantId == command.ProductVariantId 
                    && i.WarehouseId == command.WarehouseId, cancellationToken);

            if (existing != null)
                throw new DomainException("Inventory item already exists for this product/variant/warehouse combination");

            var inventoryItem = new InventoryItem(command.ProductId, command.WarehouseId, command.ProductVariantId, command.QuantityOnHand, command.ReorderLevel, command.ReorderQuantity, command.AllowBackorder);

            _db.InventoryItems.Add(inventoryItem);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminInventoryDto>(inventoryItem);
        }
    }
}