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
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            order.MarkDelivered();
            await _db.SaveChangesAsync(cancellationToken);

            return new Unit();
        }
    }
}