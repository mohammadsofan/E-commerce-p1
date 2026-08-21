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

namespace Ecommerce.Application.Queries.HeroBanners
{
    public class GetActiveHeroBannerQueryHandler : IQueryHandler<GetActiveHeroBannerQuery, HeroBannerDto?>
    {
        private readonly IApplicationDbContext _db;

        public GetActiveHeroBannerQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<HeroBannerDto?> Handle(GetActiveHeroBannerQuery query, CancellationToken cancellationToken = default)
        {
            var banner = await _db.HeroBanners
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ThenByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (banner == null)
                return null;

            return new HeroBannerDto
            {
                Id = banner.Id,
                BadgeText = banner.BadgeText,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                PrimaryButtonText = banner.PrimaryButtonText,
                PrimaryButtonLink = banner.PrimaryButtonLink,
                SecondaryButtonText = banner.SecondaryButtonText,
                SecondaryButtonLink = banner.SecondaryButtonLink,
                ImageUrl = banner.ImageUrl,
                DisplayOrder = banner.DisplayOrder,
                IsActive = banner.IsActive,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            };
        }
    }

    public class GetActiveHeroBannersQueryHandler : IQueryHandler<GetActiveHeroBannersQuery, List<HeroBannerDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetActiveHeroBannersQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<HeroBannerDto>> Handle(GetActiveHeroBannersQuery query, CancellationToken cancellationToken = default)
        {
            return await _db.HeroBanners
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ThenByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .Select(banner => new HeroBannerDto
                {
                    Id = banner.Id,
                    BadgeText = banner.BadgeText,
                    Title = banner.Title,
                    Subtitle = banner.Subtitle,
                    PrimaryButtonText = banner.PrimaryButtonText,
                    PrimaryButtonLink = banner.PrimaryButtonLink,
                    SecondaryButtonText = banner.SecondaryButtonText,
                    SecondaryButtonLink = banner.SecondaryButtonLink,
                    ImageUrl = banner.ImageUrl,
                    DisplayOrder = banner.DisplayOrder,
                    IsActive = banner.IsActive,
                    CreatedAt = banner.CreatedAt,
                    UpdatedAt = banner.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }
    }

    public class GetAdminHeroBannersQueryHandler : IQueryHandler<GetAdminHeroBannersQuery, PagedResult<HeroBannerDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetAdminHeroBannersQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<HeroBannerDto>> Handle(GetAdminHeroBannersQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.HeroBanners.AsNoTracking();

            if (query.IsActive.HasValue)
            {
                q = q.Where(b => b.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(b => b.Title.ToLower().Contains(term) || b.Subtitle.ToLower().Contains(term) || b.BadgeText.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(cancellationToken);

            var items = await q
                .OrderBy(b => b.DisplayOrder)
                .ThenByDescending(b => b.IsActive)
                .ThenByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(b => new HeroBannerDto
                {
                    Id = b.Id,
                    BadgeText = b.BadgeText,
                    Title = b.Title,
                    Subtitle = b.Subtitle,
                    PrimaryButtonText = b.PrimaryButtonText,
                    PrimaryButtonLink = b.PrimaryButtonLink,
                    SecondaryButtonText = b.SecondaryButtonText,
                    SecondaryButtonLink = b.SecondaryButtonLink,
                    ImageUrl = b.ImageUrl,
                    DisplayOrder = b.DisplayOrder,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<HeroBannerDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetHeroBannerByIdQueryHandler : IQueryHandler<GetHeroBannerByIdQuery, HeroBannerDto>
    {
        private readonly IApplicationDbContext _db;

        public GetHeroBannerByIdQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<HeroBannerDto> Handle(GetHeroBannerByIdQuery query, CancellationToken cancellationToken = default)
        {
            var banner = await _db.HeroBanners
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.Id, cancellationToken);

            if (banner == null)
            {
                throw new NotFoundException("HeroBanner", query.Id);
            }

            return new HeroBannerDto
            {
                Id = banner.Id,
                BadgeText = banner.BadgeText,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                PrimaryButtonText = banner.PrimaryButtonText,
                PrimaryButtonLink = banner.PrimaryButtonLink,
                SecondaryButtonText = banner.SecondaryButtonText,
                SecondaryButtonLink = banner.SecondaryButtonLink,
                ImageUrl = banner.ImageUrl,
                DisplayOrder = banner.DisplayOrder,
                IsActive = banner.IsActive,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            };
        }
    }
}
