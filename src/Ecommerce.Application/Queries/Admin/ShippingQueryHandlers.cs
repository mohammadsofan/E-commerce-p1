using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminShippingZonesQueryHandler : IQueryHandler<GetAdminShippingZonesQuery, PagedResult<AdminShippingZoneDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShippingZonesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminShippingZoneDto>> Handle(GetAdminShippingZonesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.ShippingZones.AsQueryable();

            if (query.IsActive.HasValue)
                q = q.Where(z => z.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(z => z.Name.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(cancellationToken);

            var zones = await q
                .Include(z => z.Locations)
                .Include(z => z.Methods)
                    .ThenInclude(m => m.Rates)
                .OrderBy(z => z.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = zones.Select(z =>
            {
                var dto = _mapper.Map<AdminShippingZoneDto>(z);
                dto.Locations = z.Locations.Select(_mapper.Map<AdminShippingZoneLocationDto>).ToList();
                dto.Methods = z.Methods.Select(_mapper.Map<AdminShippingMethodDto>).ToList();
                return dto;
            }).ToList();

            return new PagedResult<AdminShippingZoneDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminShippingZoneByIdQueryHandler : IQueryHandler<GetAdminShippingZoneByIdQuery, AdminShippingZoneDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShippingZoneByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingZoneDto> Handle(GetAdminShippingZoneByIdQuery query, CancellationToken cancellationToken = default)
        {
            var zone = await _db.ShippingZones
                .Include(z => z.Locations)
                .Include(z => z.Methods)
                    .ThenInclude(m => m.Rates)
                .FirstOrDefaultAsync(z => z.Id == query.Id, cancellationToken);

            if (zone == null)
                throw new Domain.Exceptions.NotFoundException("ShippingZone", query.Id);

            var dto = _mapper.Map<AdminShippingZoneDto>(zone);
            dto.Locations = zone.Locations.Select(_mapper.Map<AdminShippingZoneLocationDto>).ToList();
            dto.Methods = zone.Methods.Select(_mapper.Map<AdminShippingMethodDto>).ToList();
            return dto;
        }
    }

    public class GetAdminShippingMethodsQueryHandler : IQueryHandler<GetAdminShippingMethodsQuery, PagedResult<AdminShippingMethodDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShippingMethodsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminShippingMethodDto>> Handle(GetAdminShippingMethodsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.ShippingMethods.AsQueryable();

            if (query.ShippingZoneId.HasValue)
                q = q.Where(m => m.ShippingZoneId == query.ShippingZoneId.Value);

            if (query.IsActive.HasValue)
                q = q.Where(m => m.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(m => m.Name.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(cancellationToken);

            var methods = await q
                .Include(m => m.Rates)
                .OrderBy(m => m.DisplayOrder)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = methods.Select(m =>
            {
                var dto = _mapper.Map<AdminShippingMethodDto>(m);
                dto.Rates = m.Rates.Select(_mapper.Map<AdminShippingRateDto>).ToList();
                return dto;
            }).ToList();

            return new PagedResult<AdminShippingMethodDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminShippingMethodByIdQueryHandler : IQueryHandler<GetAdminShippingMethodByIdQuery, AdminShippingMethodDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShippingMethodByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingMethodDto> Handle(GetAdminShippingMethodByIdQuery query, CancellationToken cancellationToken = default)
        {
            var method = await _db.ShippingMethods
                .Include(m => m.Rates)
                .FirstOrDefaultAsync(m => m.Id == query.Id, cancellationToken);

            if (method == null)
                throw new Domain.Exceptions.NotFoundException("ShippingMethod", query.Id);

            var dto = _mapper.Map<AdminShippingMethodDto>(method);
            dto.Rates = method.Rates.Select(_mapper.Map<AdminShippingRateDto>).ToList();
            return dto;
        }
    }

    public class GetAdminShippingRatesQueryHandler : IQueryHandler<GetAdminShippingRatesQuery, PagedResult<AdminShippingRateDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShippingRatesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminShippingRateDto>> Handle(GetAdminShippingRatesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.ShippingRates.AsQueryable();

            if (query.ShippingMethodId.HasValue)
                q = q.Where(r => r.ShippingMethodId == query.ShippingMethodId.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var rates = await q
                .OrderBy(r => r.ConditionType)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminShippingRateDto>
            {
                Items = _mapper.Map<System.Collections.Generic.List<AdminShippingRateDto>>(rates),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminShippingRateByIdQueryHandler : IQueryHandler<GetAdminShippingRateByIdQuery, AdminShippingRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShippingRateByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminShippingRateDto> Handle(GetAdminShippingRateByIdQuery query, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ShippingRates.FindAsync(new object[] { query.Id }, cancellationToken);
            if (rate == null)
                throw new Domain.Exceptions.NotFoundException("ShippingRate", query.Id);

            return _mapper.Map<AdminShippingRateDto>(rate);
        }
    }
}