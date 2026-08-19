using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminProductsQueryHandler : IQueryHandler<GetAdminProductsQuery, PagedResult<AdminProductDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminProductsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminProductDto>> Handle(GetAdminProductsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Include(p => p.InventoryItems)
                .AsQueryable();

            if (!query.IncludeDeleted)
                q = q.Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                q = q.Where(p => p.Name.Contains(query.Search) || 
                                p.Sku.Contains(query.Search) || 
                                p.Slug.Contains(query.Search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(p => p.Status == query.Status);

            if (query.BrandId.HasValue)
                q = q.Where(p => p.BrandId == query.BrandId);

            if (query.IsActive.HasValue)
                q = q.Where(p => p.IsActive == query.IsActive.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var products = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var items = _mapper.Map<List<AdminProductDto>>(products);

            return new PagedResult<AdminProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}