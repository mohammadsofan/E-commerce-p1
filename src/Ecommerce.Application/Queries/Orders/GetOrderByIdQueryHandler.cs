using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Orders
{
    public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetOrderByIdQueryHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<OrderDto> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.IsAdmin;
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

            if (order == null)
                throw new NotFoundException("Order", query.Id);

            if (!isAdmin && (!userId.HasValue || order.UserId != userId.Value))
                throw new NotFoundException("Order", query.Id);

            var dto = _mapper.Map<OrderDto>(order);

            var (address, paymentMethod) = ParseOrderNotes(order.Notes);
            dto.ShippingAddress = address;
            dto.PaymentMethod = paymentMethod;

            if (order.UserId.HasValue)
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.UserId.Value, cancellationToken);
                if (user != null)
                {
                    var fullName = $"{user.FirstName} {user.LastName}".Trim();
                    dto.CustomerName = !string.IsNullOrWhiteSpace(fullName)
                        ? fullName
                        : (!string.IsNullOrWhiteSpace(user.DisplayName)
                            ? user.DisplayName
                            : (!string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : user.Email));
                    dto.CustomerEmail = user.Email ?? string.Empty;
                    dto.CustomerPhone = user.PhoneNumber ?? string.Empty;
                }
            }

            return dto;
        }

        private static (string address, string paymentMethod) ParseOrderNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return (string.Empty, string.Empty);
            var parts = notes.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
            string address = string.Empty;
            string paymentMethod = string.Empty;
            foreach (var part in parts)
            {
                if (part.StartsWith("Address: ", StringComparison.OrdinalIgnoreCase))
                    address = part.Substring("Address: ".Length).Trim();
                else if (part.StartsWith("PaymentMethod: ", StringComparison.OrdinalIgnoreCase))
                    paymentMethod = part.Substring("PaymentMethod: ".Length).Trim();
            }
            return (address, paymentMethod);
        }
    }
}