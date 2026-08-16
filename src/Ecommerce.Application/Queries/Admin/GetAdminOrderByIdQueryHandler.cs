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

            return _mapper.Map<OrderDto>(order);
        }
    }
}