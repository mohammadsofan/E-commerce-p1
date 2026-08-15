using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Commands.ReserveInventory
{
    public class ReserveInventoryCommandHandler
    {
        private readonly IApplicationDbContext _db;

        public ReserveInventoryCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task Handle(ReserveInventoryCommand command, CancellationToken cancellationToken = default)
        {
            if (command.Quantity <= 0) throw new InventoryException("Quantity must be positive");

            var item = await _db.InventoryItems.FindAsync(new object[] { command.InventoryItemId }, cancellationToken);
            if (item == null) throw new InventoryException("Inventory item not found");

            item.Reserve(command.Quantity);

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
