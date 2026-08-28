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
    public class GetAdminShipmentsQueryHandler : IQueryHandler<GetAdminShipmentsQuery, PagedResult<ShipmentDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShipmentsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<ShipmentDto>> Handle(GetAdminShipmentsQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.Shipments
                .Include(s => s.Items)
                .AsNoTracking()
                .AsQueryable();

            if (query.OrderId.HasValue)
                q = q.Where(s => s.OrderId == query.OrderId.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(s => s.Status == query.Status);

            var totalCount = await q.CountAsync(cancellationToken);

            var shipments = await q
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var warehouses = await _db.Warehouses.AsNoTracking().ToListAsync(cancellationToken);

            var items = new List<ShipmentDto>();
            foreach (var shipment in shipments)
            {
                var dto = _mapper.Map<ShipmentDto>(shipment);
                dto.WarehouseName = warehouses.FirstOrDefault(w => w.Id == shipment.WarehouseId)?.Name ?? string.Empty;
                items.Add(dto);
            }

            return new PagedResult<ShipmentDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public class GetAdminShipmentByIdQueryHandler : IQueryHandler<GetAdminShipmentByIdQuery, ShipmentDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminShipmentByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ShipmentDto> Handle(GetAdminShipmentByIdQuery query, CancellationToken cancellationToken = default)
        {
            var shipment = await _db.Shipments
                .Include(s => s.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken);
            if (shipment == null)
                throw new NotFoundException("Shipment", query.Id);

            var dto = _mapper.Map<ShipmentDto>(shipment);
            dto.WarehouseName = (await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == shipment.WarehouseId, cancellationToken))?.Name ?? string.Empty;
            return dto;
        }
    }

    public class GetOrderShipmentQueryHandler : IQueryHandler<GetOrderShipmentQuery, ShipmentDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetOrderShipmentQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ShipmentDto> Handle(GetOrderShipmentQuery query, CancellationToken cancellationToken = default)
        {
            var shipment = await _db.Shipments
                .Include(s => s.Items)
                .AsNoTracking()
                .Where(s => s.OrderId == query.OrderId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (shipment == null)
                throw new NotFoundException("Shipment", query.OrderId);

            var dto = _mapper.Map<ShipmentDto>(shipment);
            dto.WarehouseName = (await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == shipment.WarehouseId, cancellationToken))?.Name ?? string.Empty;
            return dto;
        }
    }
}