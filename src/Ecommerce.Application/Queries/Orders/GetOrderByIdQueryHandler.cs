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
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == query.Id && (userId == null || o.UserId == userId), cancellationToken);

            if (order == null)
                throw new NotFoundException("Order", query.Id);

            return _mapper.Map<OrderDto>(order);
        }
    }
}