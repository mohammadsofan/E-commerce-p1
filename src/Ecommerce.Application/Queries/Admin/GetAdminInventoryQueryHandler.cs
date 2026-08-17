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
    public class GetAdminInventoryQueryHandler : IQueryHandler<GetAdminInventoryQuery, PagedResult<AdminInventoryDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminInventoryQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminInventoryDto>> Handle(GetAdminInventoryQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.InventoryItems
                .Include(i => i.Product)
                .Include(i => i.ProductVariant)
                .Include(i => i.Warehouse)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                q = q.Where(i => i.Product.Name.Contains(query.Search) ||
                                i.Product.Sku.Contains(query.Search) ||
                                (i.ProductVariant != null && i.ProductVariant.Sku.Contains(query.Search)));
            }

            if (query.ProductId.HasValue)
                q = q.Where(i => i.ProductId == query.ProductId.Value);

            if (query.WarehouseId.HasValue)
                q = q.Where(i => i.WarehouseId == query.WarehouseId.Value);

            if (query.LowStockOnly == true)
                q = q.Where(i => i.Available <= i.ReorderLevel && i.ReorderLevel > 0);

            if (!query.IncludeBackorder)
                q = q.Where(i => !i.AllowBackorder || i.Available >= 0);

            var totalCount = await q.CountAsync(cancellationToken);

            var items = await q
                .OrderByDescending(i => i.UpdatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var itemsDto = new List<AdminInventoryDto>();
            foreach (var item in items)
            {
                var dto = _mapper.Map<AdminInventoryDto>(item);
                dto.ProductName = item.Product?.Name ?? string.Empty;
                dto.VariantName = item.ProductVariant?.Name ?? string.Empty;
                dto.Sku = item.ProductVariant?.Sku ?? item.Product?.Sku ?? string.Empty;
                dto.WarehouseName = item.Warehouse?.Name ?? string.Empty;
                itemsDto.Add(dto);
            }

            return new PagedResult<AdminInventoryDto>
            {
                Items = itemsDto,
                TotalCount = itemsDto.Count,
                Page = 1,
                PageSize = query.PageSize
            };
        }
    }
}