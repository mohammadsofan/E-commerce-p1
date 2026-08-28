using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Inventory;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class MarkOrderDeliveredCommandHandler : ICommandHandler<MarkOrderDeliveredCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public MarkOrderDeliveredCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(MarkOrderDeliveredCommand command, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            order.MarkDelivered();

            // The goods have physically left the warehouse: turn each reservation into an
            // on-hand deduction so QuantityOnHand reflects real stock and the reservation
            // is not held forever.
            await ConsumeReservationsAsync(order, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new Unit();
        }

        private async Task ConsumeReservationsAsync(Domain.Entities.Order order, CancellationToken cancellationToken)
        {
            if (order.Items == null || !order.Items.Any()) return;

            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var variantIds = order.Items
                .Where(i => i.ProductVariantId != Guid.Empty)
                .Select(i => i.ProductVariantId)
                .Distinct()
                .ToList();

            var inventoryItems = await _db.InventoryItems
                .Where(inv => productIds.Contains(inv.ProductId) ||
                              (inv.ProductVariantId.HasValue && variantIds.Contains(inv.ProductVariantId.Value)))
                .ToListAsync(cancellationToken);

            foreach (var item in order.Items)
            {
                var variantId = item.ProductVariantId == Guid.Empty ? (Guid?)null : item.ProductVariantId;
                var candidates = InventoryAllocator.CandidatesFor(inventoryItems, item.ProductId, variantId);
                InventoryAllocator.ConsumeReservation(candidates, item.Quantity);
            }
        }
    }
}
