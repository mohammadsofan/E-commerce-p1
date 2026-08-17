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
    public class GetAdminVendorsQueryHandler : IQueryHandler<GetAdminVendorsQuery, PagedResult<VendorDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminVendorsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<VendorDto>> Handle(GetAdminVendorsQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.Vendors.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                q = q.Where(v => v.Name.ToLower().Contains(term) || v.Code.ToLower().Contains(term));
            }
            if (query.IsActive.HasValue)
                q = q.Where(v => v.IsActive == query.IsActive.Value);

            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderBy(v => v.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<VendorDto>
            {
                Items = _mapper.Map<List<VendorDto>>(items),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public class GetAdminVendorByIdQueryHandler : IQueryHandler<GetAdminVendorByIdQuery, VendorDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminVendorByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<VendorDto> Handle(GetAdminVendorByIdQuery query, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == query.Id, cancellationToken);
            if (vendor == null)
                throw new DomainException("Vendor not found");

            return _mapper.Map<VendorDto>(vendor);
        }
    }

    public class GetVendorProductsQueryHandler : IQueryHandler<GetVendorProductsQuery, List<VendorProductDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetVendorProductsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<VendorProductDto>> Handle(GetVendorProductsQuery query, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == query.VendorId, cancellationToken);

            var items = await _db.VendorProducts
                .AsNoTracking()
                .Where(vp => vp.VendorId == query.VendorId)
                .ToListAsync(cancellationToken);

            var productNames = await _db.Products
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var dtos = _mapper.Map<List<VendorProductDto>>(items);
            foreach (var dto in dtos)
            {
                dto.VendorName = vendor?.Name ?? string.Empty;
                dto.ProductName = productNames.TryGetValue(dto.ProductId, out var name)
                    ? name
                    : dto.ProductId.ToString();
            }

            return dtos;
        }
    }
}