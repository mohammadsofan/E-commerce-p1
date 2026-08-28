using System;
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
            await OrderReservationService.ConsumeAsync(_db, order, cancellationToken);

            // Keep the tracking record in step with the order so customer tracking shows Delivered.
            var shipment = await _db.Shipments
                .Where(s => s.OrderId == order.Id)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (shipment != null)
            {
                shipment.Status = "Delivered";
                shipment.DeliveredAt = DateTimeOffset.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new Unit();
        }
    }
}
