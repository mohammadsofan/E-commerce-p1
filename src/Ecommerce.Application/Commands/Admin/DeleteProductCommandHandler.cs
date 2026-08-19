using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Commands.Admin
{
    public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly IProductSearchService? _searchService;

        public DeleteProductCommandHandler(IApplicationDbContext db, IProductSearchService? searchService = null)
        {
            _db = db;
            _searchService = searchService;
        }

        public async Task<Unit> Handle(DeleteProductCommand command, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (product == null)
                throw new NotFoundException("Product", command.Id);

            if (command.HardDelete)
            {
                var inventoryItems = await _db.InventoryItems
                    .Where(i => i.ProductId == product.Id)
                    .ToListAsync(cancellationToken);
                _db.InventoryItems.RemoveRange(inventoryItems);
                _db.Products.Remove(product);
            }
            else
            {
                product.IsDeleted = true;
                product.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (_searchService != null)
            {
                if (command.HardDelete)
                {
                    await _searchService.RemoveFromIndexAsync(product.Id, cancellationToken);
                }
                else
                {
                    await _searchService.IndexProductAsync(product.Id, cancellationToken);
                }
            }

            return new Unit();
        }
    }
}
