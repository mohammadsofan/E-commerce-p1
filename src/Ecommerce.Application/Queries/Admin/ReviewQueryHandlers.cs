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
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetProductReviewsQueryHandler : IQueryHandler<GetProductReviewsQuery, List<ProductReviewDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetProductReviewsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<ProductReviewDto>> Handle(GetProductReviewsQuery query, CancellationToken cancellationToken = default)
        {
            var reviews = await _db.ProductReviews
                .AsNoTracking()
                .Where(r => r.ProductId == query.ProductId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            return await ReviewMapper.MapWithDisplayNamesAsync(_db, _mapper, reviews, cancellationToken);
        }
    }

    public class GetAdminReviewsQueryHandler : IQueryHandler<GetAdminReviewsQuery, PagedResult<ProductReviewDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminReviewsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<ProductReviewDto>> Handle(GetAdminReviewsQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.ProductReviews.AsNoTracking().AsQueryable();
            if (query.ProductId.HasValue)
                q = q.Where(r => r.ProductId == query.ProductId.Value);
            if (query.IsApproved.HasValue)
                q = q.Where(r => r.IsApproved == query.IsApproved.Value);
            if (query.MinRating.HasValue)
                q = q.Where(r => r.Rating >= query.MinRating.Value);

            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ProductReviewDto>
            {
                Items = await ReviewMapper.MapWithDisplayNamesAsync(_db, _mapper, items, cancellationToken),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    internal static class ReviewMapper
    {
        public static async Task<List<ProductReviewDto>> MapWithDisplayNamesAsync(
            IApplicationDbContext db, IMapper mapper, List<Domain.Entities.ProductReview> reviews, CancellationToken cancellationToken)
        {
            var dto = mapper.Map<List<ProductReviewDto>>(reviews);

            var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
            var users = await db.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.DisplayName) ? u.UserName : u.DisplayName, cancellationToken);

            foreach (var item in dto)
            {
                if (users.TryGetValue(item.UserId, out var displayName))
                    item.UserDisplayName = displayName;
            }

            return dto;
        }
    }
}