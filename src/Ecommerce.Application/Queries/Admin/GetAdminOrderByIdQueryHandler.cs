using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminOrderByIdQueryHandler : IQueryHandler<GetAdminOrderByIdQuery, OrderDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminOrderByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<OrderDto> Handle(GetAdminOrderByIdQuery query, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

            if (order == null)
                throw new NotFoundException("Order", query.Id);

            var dto = _mapper.Map<OrderDto>(order);

            var (address, paymentMethod) = ParseOrderNotes(order.Notes);
            dto.ShippingAddress = address;
            dto.PaymentMethod = !string.IsNullOrWhiteSpace(order.PaymentMethod) ? order.PaymentMethod : paymentMethod;

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