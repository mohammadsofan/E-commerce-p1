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
    public class GetAdminInventoryByIdQueryHandler : IQueryHandler<GetAdminInventoryByIdQuery, AdminInventoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminInventoryByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminInventoryDto> Handle(GetAdminInventoryByIdQuery query, CancellationToken cancellationToken = default)
        {
            var item = await _db.InventoryItems
                .Include(i => i.Product)
                .Include(i => i.ProductVariant)
                .Include(i => i.Warehouse)
                .FirstOrDefaultAsync(i => i.Id == query.Id, cancellationToken);

            if (item == null)
                throw new NotFoundException("InventoryItem", query.Id);

            var dto = _mapper.Map<AdminInventoryDto>(item);
            dto.ProductName = item.Product?.Name ?? string.Empty;
            dto.VariantName = item.ProductVariant?.Name ?? string.Empty;
            dto.Sku = item.ProductVariant?.Sku ?? item.Product?.Sku ?? string.Empty;
            dto.WarehouseName = item.Warehouse?.Name ?? string.Empty;
            return dto;
        }
    }
}