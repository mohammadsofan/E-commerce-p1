using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class TransferInventoryCommandHandler : ICommandHandler<TransferInventoryCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public TransferInventoryCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(TransferInventoryCommand command, CancellationToken cancellationToken = default)
        {
            if (command.FromWarehouseId == command.ToWarehouseId)
                throw new DomainException("Source and destination warehouses must be different");

            if (command.Quantity <= 0)
                throw new DomainException("Transfer quantity must be positive");

            var sourceItem = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == command.InventoryItemId && i.WarehouseId == command.FromWarehouseId, cancellationToken);

            if (sourceItem == null)
                throw new NotFoundException("InventoryItem", command.InventoryItemId);

            // Check if destination inventory item exists
            var destItem = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == sourceItem.ProductId 
                    && i.ProductVariantId == sourceItem.ProductVariantId 
                    && i.WarehouseId == command.ToWarehouseId, cancellationToken);

            if (destItem == null)
            {
                // Create new inventory item at destination
                destItem = new Ecommerce.Domain.Entities.InventoryItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = sourceItem.ProductId,
                    ProductVariantId = sourceItem.ProductVariantId,
                    WarehouseId = command.ToWarehouseId,
                    ReorderLevel = sourceItem.ReorderLevel,
                    ReorderQuantity = sourceItem.ReorderQuantity,
                    AllowBackorder = sourceItem.AllowBackorder
                };
                await _db.InventoryItems.AddAsync(destItem, cancellationToken);
            }

            // Check available stock at source
            if (sourceItem.Available < command.Quantity)
                throw new DomainException($"Insufficient stock at source warehouse. Available: {sourceItem.Available}");

            // Perform transfer
            sourceItem.Release(command.Quantity);
            destItem.AddStock(command.Quantity);

            await _db.SaveChangesAsync(cancellationToken);
            return new Unit();
        }
    }
}