using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminOrdersQueryHandler : IQueryHandler<GetAdminOrdersQuery, PagedResult<OrderDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminOrdersQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<OrderDto>> Handle(GetAdminOrdersQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.Orders
                .Include(o => o.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                var matchingUserIds = await _db.Users
                    .Where(u => u.Email.Contains(term) ||
                                u.FirstName.Contains(term) ||
                                u.LastName.Contains(term) ||
                                u.DisplayName.Contains(term) ||
                                u.PhoneNumber.Contains(term))
                    .Select(u => (Guid?)u.Id)
                    .ToListAsync(cancellationToken);

                q = q.Where(o => o.OrderNumber.Contains(term) ||
                                (o.UserId.HasValue && matchingUserIds.Contains(o.UserId)) ||
                                o.Notes.Contains(term) ||
                                o.CustomerNotes.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(o => o.Status.ToString() == query.Status);

            if (!string.IsNullOrWhiteSpace(query.PaymentStatus))
                q = q.Where(o => o.PaymentStatus.ToString() == query.PaymentStatus);

            if (!string.IsNullOrWhiteSpace(query.FulfillmentStatus))
                q = q.Where(o => o.FulfillmentStatus.ToString() == query.FulfillmentStatus);

            if (query.UserId.HasValue)
                q = q.Where(o => o.UserId == query.UserId);

            if (query.FromDate.HasValue)
                q = q.Where(o => o.CreatedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(o => o.CreatedAt <= query.ToDate.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var orders = await q
                .OrderByDescending(o => o.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = orders.Where(o => o.UserId.HasValue).Select(o => o.UserId!.Value).Distinct().ToList();
            var users = userIds.Any()
                ? await _db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken)
                : new List<IApplicationUser>();

            var userDict = users.ToDictionary(u => u.Id);

            var items = _mapper.Map<List<OrderDto>>(orders);
            foreach (var item in items)
            {
                var matchingOrder = orders.FirstOrDefault(o => o.Id == item.Id);
                if (matchingOrder != null)
                {
                    var (address, paymentMethod) = ParseOrderNotes(matchingOrder.Notes);
                    item.ShippingAddress = address;
                    item.PaymentMethod = paymentMethod;
                }

                if (item.UserId.HasValue && userDict.TryGetValue(item.UserId.Value, out var u))
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    item.CustomerName = !string.IsNullOrWhiteSpace(fullName)
                        ? fullName
                        : (!string.IsNullOrWhiteSpace(u.DisplayName)
                            ? u.DisplayName
                            : (!string.IsNullOrWhiteSpace(u.UserName) ? u.UserName : u.Email));
                    item.CustomerEmail = u.Email ?? string.Empty;
                    item.CustomerPhone = u.PhoneNumber ?? string.Empty;
                }
            }

            return new PagedResult<OrderDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
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