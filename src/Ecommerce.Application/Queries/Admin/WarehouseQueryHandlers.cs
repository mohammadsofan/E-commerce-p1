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
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminWarehousesQueryHandler : IQueryHandler<GetAdminWarehousesQuery, PagedResult<WarehouseDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminWarehousesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<WarehouseDto>> Handle(GetAdminWarehousesQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.Warehouses.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                q = q.Where(w => w.Name.ToLower().Contains(term) || w.Code.ToLower().Contains(term));
            }
            if (query.IsActive.HasValue)
                q = q.Where(w => w.IsActive == query.IsActive.Value);

            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderBy(w => w.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<WarehouseDto>
            {
                Items = _mapper.Map<List<WarehouseDto>>(items),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public class GetAdminWarehouseByIdQueryHandler : IQueryHandler<GetAdminWarehouseByIdQuery, WarehouseDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminWarehouseByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<WarehouseDto> Handle(GetAdminWarehouseByIdQuery query, CancellationToken cancellationToken = default)
        {
            var warehouse = await _db.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == query.Id, cancellationToken);
            if (warehouse == null)
                throw new DomainException("Warehouse not found");

            return _mapper.Map<WarehouseDto>(warehouse);
        }
    }
}
