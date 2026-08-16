using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class MarkOrderShippedCommandHandler : ICommandHandler<MarkOrderShippedCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public MarkOrderShippedCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(MarkOrderShippedCommand command, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            order.MarkShipped(command.TrackingNumber, command.Carrier);
            await _db.SaveChangesAsync(cancellationToken);

            return new Unit();
        }
    }
}