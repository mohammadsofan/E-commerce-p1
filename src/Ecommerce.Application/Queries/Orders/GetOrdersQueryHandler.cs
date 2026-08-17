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

            return await q
                .Select(o => _mapper.Map<OrderDto>(o))
                .ToListAsync(cancellationToken);
        }
    }
}
