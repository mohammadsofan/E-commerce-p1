using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Queries.Orders
{
    public class GetOrdersQueryHandler : IQueryHandler<GetOrdersQuery, List<OrderDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetOrdersQueryHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<List<OrderDto>> Handle(GetOrdersQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var userId = _currentUser.UserId;
            var q = _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => userId.HasValue && o.UserId == userId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var orders = await q.ToListAsync(cancellationToken);
            var dtos = _mapper.Map<List<OrderDto>>(orders);
            foreach (var dto in dtos)
            {
                var matchingOrder = orders.FirstOrDefault(o => o.Id == dto.Id);
                if (matchingOrder != null)
                {
                    var (address, paymentMethod) = ParseOrderNotes(matchingOrder.Notes);
                    dto.ShippingAddress = address;
                    dto.PaymentMethod = !string.IsNullOrWhiteSpace(matchingOrder.PaymentMethod) ? matchingOrder.PaymentMethod : paymentMethod;
                }
            }

            return dtos;
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
