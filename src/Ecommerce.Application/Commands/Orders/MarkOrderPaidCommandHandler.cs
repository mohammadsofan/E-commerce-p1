using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Orders
{
    public class MarkOrderPaidCommandHandler : ICommandHandler<MarkOrderPaidCommand, OrderDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public MarkOrderPaidCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<OrderDto> Handle(MarkOrderPaidCommand command, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            // Transitions Placed -> Paid (enforced inside the aggregate).
            order.MarkPaid();
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<OrderDto>(order);
        }
    }
}
