using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Inventory;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Orders
{
    public class CompleteOrderCommandHandler : ICommandHandler<CompleteOrderCommand, OrderDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public CompleteOrderCommandHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<OrderDto> Handle(CompleteOrderCommand command, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

            if (order == null) throw new NotFoundException("Order", command.OrderId);

            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;
            if (!isAdmin && (!userId.HasValue || order.UserId != userId.Value))
                throw new NotFoundException("Order", command.OrderId);

            // Transitions Paid -> Completed (enforced inside the aggregate).
            var wasDelivered = order.FulfillmentStatus == FulfillmentStatus.Delivered;

            order.Complete();

            // Completing marks the order Delivered, so the goods have left the warehouse: turn the
            // remaining reservations into an on-hand deduction. A previously delivered order has
            // already had its reservation consumed, so it must not be consumed a second time.
            if (!wasDelivered)
            {
                await OrderReservationService.ConsumeAsync(_db, order, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<OrderDto>(order);
        }
    }
}
