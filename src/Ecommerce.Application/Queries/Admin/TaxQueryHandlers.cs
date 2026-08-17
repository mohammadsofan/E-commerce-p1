using System;
using System.Collections.Generic;
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
    public class GetAdminTaxCategoriesQueryHandler : IQueryHandler<GetAdminTaxCategoriesQuery, PagedResult<AdminTaxCategoryDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminTaxCategoriesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminTaxCategoryDto>> Handle(GetAdminTaxCategoriesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.TaxCategories.AsQueryable();

            if (query.IsActive.HasValue)
                q = q.Where(c => c.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(c => c.Name.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(cancellationToken);

            var categories = await q
                .Include(c => c.Rates)
                .OrderBy(c => c.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = categories.Select(c =>
            {
                var dto = _mapper.Map<AdminTaxCategoryDto>(c);
                dto.Rates = c.Rates.Select(_mapper.Map<AdminTaxRateDto>).ToList();
                return dto;
            }).ToList();

            return new PagedResult<AdminTaxCategoryDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminTaxCategoryByIdQueryHandler : IQueryHandler<GetAdminTaxCategoryByIdQuery, AdminTaxCategoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminTaxCategoryByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminTaxCategoryDto> Handle(GetAdminTaxCategoryByIdQuery query, CancellationToken cancellationToken = default)
        {
            var category = await _db.TaxCategories
                .Include(c => c.Rates)
                .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

            if (category == null)
                throw new Domain.Exceptions.NotFoundException("TaxCategory", query.Id);

            var dto = _mapper.Map<AdminTaxCategoryDto>(category);
            dto.Rates = category.Rates.Select(_mapper.Map<AdminTaxRateDto>).ToList();
            return dto;
        }
    }

    public class GetAdminTaxRatesQueryHandler : IQueryHandler<GetAdminTaxRatesQuery, PagedResult<AdminTaxRateDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminTaxRatesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminTaxRateDto>> Handle(GetAdminTaxRatesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.TaxRates.AsQueryable();

            if (query.TaxCategoryId.HasValue)
                q = q.Where(r => r.TaxCategoryId == query.TaxCategoryId.Value);

            if (query.IsActive.HasValue)
                q = q.Where(r => r.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.CountryCode))
                q = q.Where(r => r.CountryCode == query.CountryCode);

            var totalCount = await q.CountAsync(cancellationToken);

            var rates = await q
                .OrderBy(r => r.CountryCode)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminTaxRateDto>
            {
                Items = _mapper.Map<List<AdminTaxRateDto>>(rates),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminTaxRateByIdQueryHandler : IQueryHandler<GetAdminTaxRateByIdQuery, AdminTaxRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminTaxRateByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminTaxRateDto> Handle(GetAdminTaxRateByIdQuery query, CancellationToken cancellationToken = default)
        {
            var rate = await _db.TaxRates.FindAsync(new object[] { query.Id }, cancellationToken);
            if (rate == null)
                throw new Domain.Exceptions.NotFoundException("TaxRate", query.Id);

            return _mapper.Map<AdminTaxRateDto>(rate);
        }
    }
}