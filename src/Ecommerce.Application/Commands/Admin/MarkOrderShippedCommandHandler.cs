using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Inventory;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class MarkOrderShippedCommandHandler : ICommandHandler<MarkOrderShippedCommand, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly IEmailService? _emailService;

        public MarkOrderShippedCommandHandler(IApplicationDbContext db, IEmailService? emailService = null)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task<Unit> Handle(MarkOrderShippedCommand command, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            order.MarkShipped(command.TrackingNumber, command.Carrier);

            // Fulfilment must leave a trace the customer can track: without a Shipment row
            // GET /api/orders/{id}/shipment has nothing to return.
            await CreateShipmentAsync(order, command, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            if (_emailService != null && order.UserId.HasValue && order.UserId.Value != Guid.Empty)
            {
                try
                {
                    var customerEmail = await _db.Users
                        .Where(u => u.Id == order.UserId.Value)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (!string.IsNullOrWhiteSpace(customerEmail))
                    {
                        await _emailService.SendOrderShippedAsync(order, customerEmail, cancellationToken);
                    }
                }
                catch
                {
                    // Non-blocking for status updates
                }
            }

            return new Unit();
        }

        /// <summary>
        /// Creates and associates the Shipment for this fulfilment, with one line per order item
        /// pointing at the inventory row that currently holds the reservation. The shipment is
        /// sourced from the warehouse those reservations live in.
        /// </summary>
        private async Task CreateShipmentAsync(Order order, MarkOrderShippedCommand command, CancellationToken cancellationToken)
        {
            var inventoryItems = await LoadInventoryAsync(order, cancellationToken);
            var shipmentId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var lines = new List<ShipmentItem>();
            foreach (var item in order.Items ?? Enumerable.Empty<OrderItem>())
            {
                var variantId = item.ProductVariantId == Guid.Empty ? (Guid?)null : item.ProductVariantId;
                var candidates = InventoryAllocator.CandidatesFor(inventoryItems, item.ProductId, variantId);

                // Prefer the row that actually holds the reservation for this line; fall back to
                // any row for the product so an untracked/edge-case line still gets a shipment line.
                var source = candidates.OrderByDescending(inv => inv.QuantityReserved).FirstOrDefault();
                if (source == null) continue;

                lines.Add(new ShipmentItem
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipmentId,
                    OrderItemId = item.Id,
                    InventoryItemId = source.Id,
                    Quantity = item.Quantity
                });
            }

            var warehouseId = await ResolveWarehouseIdAsync(lines, inventoryItems, cancellationToken);

            var shipment = new Shipment
            {
                Id = shipmentId,
                OrderId = order.Id,
                WarehouseId = warehouseId,
                TrackingNumber = command.TrackingNumber ?? string.Empty,
                Carrier = command.Carrier ?? string.Empty,
                Status = "Shipped",
                ShippedAt = now,
                CreatedAt = now,
                Items = lines
            };

            _db.Shipments.Add(shipment);
        }

        private async Task<List<InventoryItem>> LoadInventoryAsync(Order order, CancellationToken cancellationToken)
        {
            if (order.Items == null || !order.Items.Any()) return new List<InventoryItem>();

            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var variantIds = order.Items
                .Where(i => i.ProductVariantId != Guid.Empty)
                .Select(i => i.ProductVariantId)
                .Distinct()
                .ToList();

            return await _db.InventoryItems
                .Where(inv => productIds.Contains(inv.ProductId) ||
                              (inv.ProductVariantId.HasValue && variantIds.Contains(inv.ProductVariantId.Value)))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// The shipment is dispatched from the warehouse holding the stock for its lines. When the
        /// order has no inventory-backed line, the first active warehouse is used so the record
        /// still satisfies its required warehouse association.
        /// </summary>
        private async Task<Guid> ResolveWarehouseIdAsync(
            List<ShipmentItem> lines,
            List<InventoryItem> inventoryItems,
            CancellationToken cancellationToken)
        {
            var sourced = lines
                .Select(l => inventoryItems.FirstOrDefault(inv => inv.Id == l.InventoryItemId))
                .Where(inv => inv != null)
                .Select(inv => inv!.WarehouseId)
                .FirstOrDefault();

            if (sourced != Guid.Empty) return sourced;

            var fallback = await _db.Warehouses
                .Where(w => w.IsActive)
                .Select(w => w.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (fallback == Guid.Empty)
            {
                fallback = await _db.Warehouses
                    .Select(w => w.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (fallback == Guid.Empty)
                throw new DomainException("Cannot ship an order: no warehouse is configured to dispatch from");

            return fallback;
        }
    }
}
