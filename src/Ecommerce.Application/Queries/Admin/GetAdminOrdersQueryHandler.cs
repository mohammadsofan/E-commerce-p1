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
                q = q.Where(o => o.OrderNumber.Contains(query.Search) ||
                                o.UserId.ToString().Contains(query.Search));
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

            var items = _mapper.Map<List<OrderDto>>(orders);

            return new PagedResult<OrderDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}