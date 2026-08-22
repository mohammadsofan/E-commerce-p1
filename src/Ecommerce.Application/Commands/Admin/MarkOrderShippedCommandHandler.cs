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
    }
}