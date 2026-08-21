using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Shipping
{
    public class GetActiveShippingMethodsQueryHandler : IQueryHandler<GetActiveShippingMethodsQuery, List<ShippingMethodDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetActiveShippingMethodsQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<ShippingMethodDto>> Handle(GetActiveShippingMethodsQuery query, CancellationToken cancellationToken = default)
        {
            var methods = await _db.ShippingMethods
                .Include(m => m.ShippingZone)
                .Where(m => m.IsActive && m.ShippingZone.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync(cancellationToken);

            return methods.Select(m => new ShippingMethodDto
            {
                Id = m.Id,
                ShippingZoneId = m.ShippingZoneId,
                ZoneName = m.ShippingZone?.Name ?? string.Empty,
                Name = m.Name,
                Description = m.Description,
                Type = m.Type,
                BaseRate = m.BaseRate,
                FreeShippingThreshold = m.FreeShippingThreshold,
                EstimatedDaysMin = m.EstimatedDaysMin,
                EstimatedDaysMax = m.EstimatedDaysMax,
                IsActive = m.IsActive,
                DisplayOrder = m.DisplayOrder
            }).ToList();
        }
    }

    public class GetActiveShippingZonesQueryHandler : IQueryHandler<GetActiveShippingZonesQuery, List<ShippingZoneDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetActiveShippingZonesQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<ShippingZoneDto>> Handle(GetActiveShippingZonesQuery query, CancellationToken cancellationToken = default)
        {
            var zones = await _db.ShippingZones
                .Include(z => z.Methods)
                .Where(z => z.IsActive)
                .OrderBy(z => z.Name)
                .ToListAsync(cancellationToken);

            return zones.Select(z => new ShippingZoneDto
            {
                Id = z.Id,
                Name = z.Name,
                Description = z.Description,
                IsActive = z.IsActive,
                Methods = z.Methods.Where(m => m.IsActive).OrderBy(m => m.DisplayOrder).Select(m => new ShippingMethodDto
                {
                    Id = m.Id,
                    ShippingZoneId = m.ShippingZoneId,
                    ZoneName = z.Name,
                    Name = m.Name,
                    Description = m.Description,
                    Type = m.Type,
                    BaseRate = m.BaseRate,
                    FreeShippingThreshold = m.FreeShippingThreshold,
                    EstimatedDaysMin = m.EstimatedDaysMin,
                    EstimatedDaysMax = m.EstimatedDaysMax,
                    IsActive = m.IsActive,
                    DisplayOrder = m.DisplayOrder
                }).ToList()
            }).ToList();
        }
    }
}
