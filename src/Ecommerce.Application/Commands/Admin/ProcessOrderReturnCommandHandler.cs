using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class ProcessOrderReturnCommandHandler : ICommandHandler<ProcessOrderReturnCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public ProcessOrderReturnCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(ProcessOrderReturnCommand command, CancellationToken cancellationToken = default)
        {
            if (command.OrderItemIds == null || !command.OrderItemIds.Any())
                throw new DomainException("At least one item must be returned");

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            order.ProcessReturn(command.OrderItemIds, command.Reason);
            await _db.SaveChangesAsync(cancellationToken);

            return new Unit();
        }
    }
}