using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.StoreFeatures
{
    public class GetActiveFeaturesQueryHandler : IQueryHandler<GetActiveFeaturesQuery, List<StoreFeatureDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetActiveFeaturesQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<StoreFeatureDto>> Handle(GetActiveFeaturesQuery query, CancellationToken cancellationToken = default)
        {
            return await _db.StoreFeatures
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.CreatedAt)
                .Select(f => new StoreFeatureDto
                {
                    Id = f.Id,
                    Title = f.Title,
                    Description = f.Description,
                    IconName = f.IconName,
                    DisplayOrder = f.DisplayOrder,
                    IsActive = f.IsActive,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }
    }

    public class GetAdminFeaturesQueryHandler : IQueryHandler<GetAdminFeaturesQuery, PagedResult<StoreFeatureDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetAdminFeaturesQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<StoreFeatureDto>> Handle(GetAdminFeaturesQuery query, CancellationToken cancellationToken = default)
        {
            var featuresQuery = _db.StoreFeatures.AsNoTracking();

            if (query.IsActive.HasValue)
            {
                featuresQuery = featuresQuery.Where(f => f.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                featuresQuery = featuresQuery.Where(f => f.Title.ToLower().Contains(term) || f.Description.ToLower().Contains(term));
            }

            var totalCount = await featuresQuery.CountAsync(cancellationToken);

            var items = await featuresQuery
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(f => new StoreFeatureDto
                {
                    Id = f.Id,
                    Title = f.Title,
                    Description = f.Description,
                    IconName = f.IconName,
                    DisplayOrder = f.DisplayOrder,
                    IsActive = f.IsActive,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<StoreFeatureDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetFeatureByIdQueryHandler : IQueryHandler<GetFeatureByIdQuery, StoreFeatureDto>
    {
        private readonly IApplicationDbContext _db;

        public GetFeatureByIdQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StoreFeatureDto> Handle(GetFeatureByIdQuery query, CancellationToken cancellationToken = default)
        {
            var feature = await _db.StoreFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == query.Id, cancellationToken);

            if (feature == null)
            {
                throw new NotFoundException("StoreFeature", query.Id);
            }

            return new StoreFeatureDto
            {
                Id = feature.Id,
                Title = feature.Title,
                Description = feature.Description,
                IconName = feature.IconName,
                DisplayOrder = feature.DisplayOrder,
                IsActive = feature.IsActive,
                CreatedAt = feature.CreatedAt,
                UpdatedAt = feature.UpdatedAt
            };
        }
    }
}
