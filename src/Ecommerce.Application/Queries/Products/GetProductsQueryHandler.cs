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
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, PagedResult<ProductDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IPromotionEvaluationService? _promotionEvaluator;

        public GetProductsQueryHandler(
            IApplicationDbContext db,
            IMapper mapper,
            IPromotionEvaluationService? promotionEvaluator = null)
        {
            _db = db;
            _mapper = mapper;
            _promotionEvaluator = promotionEvaluator;
        }

        public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var products = _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.InventoryItems)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantAttributes)
                        .ThenInclude(va => va.ProductAttribute)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.InventoryItems)
                .Where(p => !p.IsDeleted);

            if (query.IsActive.HasValue)
                products = products.Where(p => p.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim().ToLowerInvariant();
                products = products.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Sku.ToLower().Contains(term) ||
                    p.Slug.ToLower().Contains(term) ||
                    p.ShortDescription.ToLower().Contains(term) ||
                    (p.SeoKeywords != null && p.SeoKeywords.ToLower().Contains(term)));
            }

            // Tag filtering: SeoKeywords stores comma-separated tag names
            if (!string.IsNullOrWhiteSpace(query.Tag))
            {
                var tagName = query.Tag.Trim();
                products = products.Where(p => p.SeoKeywords != null && EF.Functions.Like(p.SeoKeywords, $"%{tagName}%"));
            }

            if (query.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == query.CategoryId.Value);

            if (query.BrandId.HasValue)
                products = products.Where(p => p.BrandId == query.BrandId.Value);

            if (query.MinPrice.HasValue)
                products = products.Where(p => p.BasePrice >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                products = products.Where(p => p.BasePrice <= query.MaxPrice.Value);

            products = ApplySorting(products, query.SortBy);

            var totalCount = await products.CountAsync(cancellationToken);

            var result = await products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = _mapper.Map<List<ProductDto>>(result);

            if (items.Count > 0 && _promotionEvaluator != null)
            {
                var targets = items.Select(i => new ProductPromotionTarget
                {
                    ProductId = i.Id,
                    CategoryId = i.Category?.Id,
                    BasePrice = i.BasePrice
                });

                var promoEvaluations = await _promotionEvaluator.EvaluateProductsAsync(targets, cancellationToken);
                foreach (var item in items)
                {
                    if (promoEvaluations.TryGetValue(item.Id, out var eval) && eval.HasActivePromotion)
                    {
                        item.PromotionalPrice = eval.PromotionalPrice;
                        item.DiscountPercentage = eval.DiscountPercentage;
                        item.PromotionName = eval.PromotionName;
                        item.PromotionBadge = eval.PromotionBadge;
                    }
                }
            }

            return new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy)
        {
            if (!string.IsNullOrEmpty(sortBy) && sortBy.ToLower() == "featured")
            {
                query = query.Where(p => p.IsFeatured);
            }

            return sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.BasePrice),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "featured" => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Name),
                "name" => query.OrderBy(p => p.Name),
                _ => query.OrderBy(p => p.Name)
            };
        }
    }
}
