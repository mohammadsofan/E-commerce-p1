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
    public class ProcessOrderRefundCommandHandler : ICommandHandler<ProcessOrderRefundCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public ProcessOrderRefundCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(ProcessOrderRefundCommand command, CancellationToken cancellationToken = default)
        {
            if (command.Amount <= 0) throw new DomainException("Refund amount must be positive");

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            order.ProcessRefund(command.Amount, command.Reason);
            await _db.SaveChangesAsync(cancellationToken);

            return new Unit();
        }
    }
}