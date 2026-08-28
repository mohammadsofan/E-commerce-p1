using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Common.Inventory
{
    /// <summary>
    /// Turns the reservations an order holds into a physical stock movement. Every terminal
    /// fulfilment transition (delivered, completed) routes through here so a reservation can
    /// never be left dangling and stock is decremented exactly once.
    /// </summary>
    public static class OrderReservationService
    {
        /// <summary>
        /// Consumes the reservation for each line of <paramref name="order"/>: the reservation is
        /// dropped and on-hand stock is reduced by the same amount, because the goods have
        /// physically left the warehouse.
        /// </summary>
        public static Task ConsumeAsync(IApplicationDbContext db, Order order, CancellationToken cancellationToken = default)
            => ApplyAsync(db, order, consume: true, cancellationToken);

        /// <summary>
        /// Releases the reservation for each line of <paramref name="order"/> without touching
        /// on-hand stock, i.e. the goods never left the warehouse.
        /// </summary>
        public static Task ReleaseAsync(IApplicationDbContext db, Order order, CancellationToken cancellationToken = default)
            => ApplyAsync(db, order, consume: false, cancellationToken);

        private static async Task ApplyAsync(IApplicationDbContext db, Order order, bool consume, CancellationToken cancellationToken)
        {
            if (order.Items == null || !order.Items.Any()) return;

            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var variantIds = order.Items
                .Where(i => i.ProductVariantId != Guid.Empty)
                .Select(i => i.ProductVariantId)
                .Distinct()
                .ToList();

            var inventoryItems = await db.InventoryItems
                .Where(inv => productIds.Contains(inv.ProductId) ||
                              (inv.ProductVariantId.HasValue && variantIds.Contains(inv.ProductVariantId.Value)))
                .ToListAsync(cancellationToken);

            foreach (var item in order.Items)
            {
                var variantId = item.ProductVariantId == Guid.Empty ? (Guid?)null : item.ProductVariantId;
                var candidates = InventoryAllocator.CandidatesFor(inventoryItems, item.ProductId, variantId);

                if (consume)
                    InventoryAllocator.ConsumeReservation(candidates, item.Quantity);
                else
                    InventoryAllocator.Release(candidates, item.Quantity);
            }
        }
    }
}
