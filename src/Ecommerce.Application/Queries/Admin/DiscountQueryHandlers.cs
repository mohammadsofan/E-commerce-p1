using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Discounts;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminCouponsQueryHandler : IQueryHandler<GetAdminCouponsQuery, PagedResult<AdminCouponDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminCouponsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminCouponDto>> Handle(GetAdminCouponsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.Coupons.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(c => c.Code.ToLower().Contains(term) || c.Description.ToLower().Contains(term));
            }

            if (query.IsActive.HasValue)
                q = q.Where(c => c.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Type))
                q = q.Where(c => c.Type == query.Type);

            var totalCount = await q.CountAsync(cancellationToken);

            var coupons = await q
                .OrderByDescending(c => c.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminCouponDto>
            {
                Items = _mapper.Map<List<AdminCouponDto>>(coupons),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminCouponByIdQueryHandler : IQueryHandler<GetAdminCouponByIdQuery, AdminCouponDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminCouponByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminCouponDto> Handle(GetAdminCouponByIdQuery query, CancellationToken cancellationToken = default)
        {
            var coupon = await _db.Coupons.FindAsync(new object[] { query.Id }, cancellationToken);

            if (coupon == null)
                throw new Domain.Exceptions.NotFoundException("Coupon", query.Id);

            return _mapper.Map<AdminCouponDto>(coupon);
        }
    }

    public class GetAdminCouponByCodeQueryHandler : IQueryHandler<GetAdminCouponByCodeQuery, AdminCouponDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminCouponByCodeQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminCouponDto> Handle(GetAdminCouponByCodeQuery query, CancellationToken cancellationToken = default)
        {
            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == query.Code.ToUpperInvariant(), cancellationToken);

            if (coupon == null)
                throw new Domain.Exceptions.NotFoundException("Coupon", query.Code);

            return _mapper.Map<AdminCouponDto>(coupon);
        }
    }

    public class GetAdminPromotionsQueryHandler : IQueryHandler<GetAdminPromotionsQuery, PagedResult<AdminPromotionDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminPromotionsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminPromotionDto>> Handle(GetAdminPromotionsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.Promotions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
            }

            if (query.IsActive.HasValue)
                q = q.Where(p => p.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Type))
                q = q.Where(p => p.Type == query.Type);

            var totalCount = await q.CountAsync(cancellationToken);

            var promotions = await q
                .OrderByDescending(p => p.Priority)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminPromotionDto>
            {
                Items = _mapper.Map<List<AdminPromotionDto>>(promotions),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminPromotionByIdQueryHandler : IQueryHandler<GetAdminPromotionByIdQuery, AdminPromotionDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminPromotionByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminPromotionDto> Handle(GetAdminPromotionByIdQuery query, CancellationToken cancellationToken = default)
        {
            var promotion = await _db.Promotions.FindAsync(new object[] { query.Id }, cancellationToken);

            if (promotion == null)
                throw new Domain.Exceptions.NotFoundException("Promotion", query.Id);

            return _mapper.Map<AdminPromotionDto>(promotion);
        }
    }

    public class ValidateCouponQueryHandler : IQueryHandler<ValidateCouponQuery, ValidateCouponResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ValidateCouponQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ValidateCouponResponse> Handle(ValidateCouponQuery query, CancellationToken cancellationToken = default)
        {
            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == query.Code.ToUpperInvariant(), cancellationToken);

            if (coupon == null)
                return new ValidateCouponResponse { IsValid = false, ErrorMessage = "كود الخصم غير صحيح" };

            if (!coupon.IsActive)
                return new ValidateCouponResponse { IsValid = false, ErrorMessage = "هذا الكوبون غير فعال" };

            var now = DateTimeOffset.UtcNow;
            if (coupon.StartAt.HasValue && coupon.StartAt.Value > now)
                return new ValidateCouponResponse { IsValid = false, ErrorMessage = "هذا الكوبون لم يبدأ تفعيله بعد" };

            if (coupon.EndAt.HasValue && coupon.EndAt.Value < now)
                return new ValidateCouponResponse { IsValid = false, ErrorMessage = "انتهت صلاحية الكوبون" };

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                return new ValidateCouponResponse { IsValid = false, ErrorMessage = "تجاوز الكوبون حد الاستخدام المسموح به" };

            if (coupon.MinOrderAmount.HasValue && query.OrderTotal < coupon.MinOrderAmount.Value)
                return new ValidateCouponResponse { IsValid = false, ErrorMessage = "لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون" };

            // Check per-user limit
            if (coupon.PerUserLimit.HasValue)
            {
                var userUsageCount = await _db.CouponUsages
                    .CountAsync(u => u.CouponId == coupon.Id && u.UserId == query.UserId, cancellationToken);

                if (userUsageCount >= coupon.PerUserLimit.Value)
                    return new ValidateCouponResponse { IsValid = false, ErrorMessage = "تجاوزت الحد الأقصى المسموح به لاستخدام هذا الكوبون" };
            }

            // Check applicable products/categories.
            // Scoping is delegated to the shared CouponDiscountCalculator so this advisory
            // endpoint answers exactly what cart-apply and checkout will do (D-02). Product
            // categories are resolved from the catalog rather than trusting the caller, so a
            // category-scoped coupon is judged correctly even when only product ids are sent.
            if (CouponDiscountCalculator.HasScoping(coupon) && (query.ProductIds.Any() || query.CategoryIds.Any()))
            {
                var categoryByProductId = query.ProductIds.Any()
                    ? await _db.Products
                        .AsNoTracking()
                        .Where(p => query.ProductIds.Contains(p.Id))
                        .Select(p => new { p.Id, p.CategoryId })
                        .ToDictionaryAsync(p => p.Id, p => p.CategoryId, cancellationToken)
                    : new Dictionary<Guid, Guid?>();

                var probeLines = query.ProductIds
                    .Select(id => new CouponLine
                    {
                        ProductId = id,
                        CategoryId = categoryByProductId.TryGetValue(id, out var categoryId) ? categoryId : null
                    })
                    .Concat(query.CategoryIds.Select(id => new CouponLine { ProductId = Guid.Empty, CategoryId = id }))
                    .ToList();

                if (!probeLines.Any(l => CouponDiscountCalculator.IsLineEligible(coupon, l.ProductId, l.CategoryId)))
                    return new ValidateCouponResponse { IsValid = false, ErrorMessage = CouponDiscountCalculator.IneligibleProductsMessage };
            }

            // Calculate discount amount. MaxDiscountAmount is honoured for every coupon type
            // (D-21) and the result can never exceed the order total (D-07).
            var discountAmount = CouponDiscountCalculator.CalculateAmount(coupon, query.OrderTotal);

            return new ValidateCouponResponse
            {
                IsValid = true,
                Coupon = _mapper.Map<AdminCouponDto>(coupon),
                DiscountAmount = discountAmount
            };
        }
    }

    public class CalculateDiscountsQueryHandler : IQueryHandler<CalculateDiscountsQuery, DiscountCalculationResult>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CalculateDiscountsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<DiscountCalculationResult> Handle(CalculateDiscountsQuery query, CancellationToken cancellationToken = default)
        {
            var result = new DiscountCalculationResult
            {
                Subtotal = query.Subtotal,
                FinalTotal = query.Subtotal
            };

            var now = DateTimeOffset.UtcNow;
            var productIds = query.Items.Select(i => i.ProductId).ToList();
            var variantIds = query.Items.Select(i => i.ProductVariantId).ToList();

            // Apply coupon if provided
            if (!string.IsNullOrWhiteSpace(query.CouponCode))
            {
                var coupon = await _db.Coupons
                    .FirstOrDefaultAsync(c => c.Code == query.CouponCode.ToUpperInvariant(), cancellationToken);

                if (coupon != null && coupon.IsActive &&
                    (!coupon.StartAt.HasValue || coupon.StartAt.Value <= now) &&
                    (!coupon.EndAt.HasValue || coupon.EndAt.Value >= now) &&
                    (!coupon.UsageLimit.HasValue || coupon.UsedCount < coupon.UsageLimit.Value) &&
                    (!coupon.MinOrderAmount.HasValue || query.Subtotal >= coupon.MinOrderAmount.Value))
                {
                    // Scoping, the max-discount cap and the zero-floor clamp are all delegated
                    // to the shared calculator, so this preview agrees with cart and checkout.
                    var categoryByProductId = productIds.Any()
                        ? await _db.Products
                            .AsNoTracking()
                            .Where(p => productIds.Contains(p.Id))
                            .Select(p => new { p.Id, p.CategoryId })
                            .ToDictionaryAsync(p => p.Id, p => p.CategoryId, cancellationToken)
                        : new Dictionary<Guid, Guid?>();

                    var couponLines = query.Items
                        .Select(i => new CouponLine
                        {
                            ProductId = i.ProductId,
                            CategoryId = categoryByProductId.TryGetValue(i.ProductId, out var categoryId) ? categoryId : null,
                            LineTotal = i.LineTotal
                        })
                        .ToList();

                    // Callers that only send ids (no per-line totals) still get a correct
                    // preview: the request-level subtotal is spread over the lines so the
                    // eligible base is never silently zero.
                    if (couponLines.Count > 0 && couponLines.Sum(l => l.LineTotal) <= 0m && query.Subtotal > 0m)
                    {
                        var perLine = Math.Round(query.Subtotal / couponLines.Count, 2, MidpointRounding.AwayFromZero);
                        couponLines = couponLines
                            .Select(l => new CouponLine { ProductId = l.ProductId, CategoryId = l.CategoryId, LineTotal = perLine })
                            .ToList();
                    }

                    var evaluation = CouponDiscountCalculator.Calculate(coupon, couponLines);
                    if (evaluation.IsApplicable && evaluation.DiscountAmount > 0)
                    {
                        var discountAmount = evaluation.DiscountAmount;

                        result.CouponDiscount = discountAmount;
                        result.TotalDiscount += discountAmount;
                        result.AppliedDiscounts.Add(new AppliedDiscount
                        {
                            Type = "coupon",
                            Code = coupon.Code,
                            Name = coupon.Description,
                            Amount = discountAmount,
                            Description = $"Coupon: {coupon.Code}"
                        });
                    }
                }
            }

            // Apply active promotions (only highest-priority promotion applies, matching checkout engine)
            var promotions = await _db.Promotions
                .Where(p => p.IsActive &&
                           (!p.StartAt.HasValue || p.StartAt.Value <= now) &&
                           (!p.EndAt.HasValue || p.EndAt.Value >= now) &&
                           (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value))
                .OrderByDescending(p => p.Priority)
                .ToListAsync(cancellationToken);

            foreach (var promo in promotions)
            {
                var promoDiscount = await CalculatePromotionDiscount(promo, query.Items, query.Subtotal, cancellationToken);
                if (promoDiscount > 0)
                {
                    result.PromotionDiscount = promoDiscount;
                    result.TotalDiscount += promoDiscount;
                    result.AppliedDiscounts.Add(new AppliedDiscount
                    {
                        Type = "promotion",
                        Code = promo.Id.ToString(),
                        Name = promo.Name,
                        Amount = promoDiscount,
                        Description = $"Promotion: {promo.Name}"
                    });
                    break;
                }
            }

            result.FinalTotal = result.Subtotal - result.TotalDiscount;
            if (result.FinalTotal < 0) result.FinalTotal = 0;

            return result;
        }

        private async Task<decimal> CalculatePromotionDiscount(Promotion promotion, List<CartItemDto> items, decimal subtotal, CancellationToken cancellationToken)
        {
            // This is a simplified implementation - in production you'd parse RulesJson
            // and implement specific promotion types (buy_x_get_y, tiered_discount, etc.)
            
            var applicableItems = items.Where(i => 
            {
                // Check applicable products
                if (!string.IsNullOrEmpty(promotion.ApplicableProductIds))
                {
                    var applicableIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(promotion.ApplicableProductIds) ?? new List<Guid>();
                    if (!applicableIds.Contains(i.ProductId))
                        return false;
                }

                // Check excluded products
                if (!string.IsNullOrEmpty(promotion.ExcludedProductIds))
                {
                    var excludedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(promotion.ExcludedProductIds) ?? new List<Guid>();
                    if (excludedIds.Contains(i.ProductId))
                        return false;
                }

                return true;
            }).ToList();

            if (!applicableItems.Any())
                return 0;

            // Simple percentage discount based on promotion type
            // In reality, you'd parse RulesJson for complex logic
            if (promotion.Type == "percentage_discount")
            {
                try
                {
                    var rules = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(promotion.RulesJson);
                    if (rules != null && rules.TryGetValue("percentage", out var percentageObj) && decimal.TryParse(percentageObj.ToString(), out var percentage))
                    {
                        var applicableSubtotal = applicableItems.Sum(i => i.LineTotal);
                        return applicableSubtotal * (percentage / 100m);
                    }
                }
                catch
                {
                    // Invalid rules JSON
                }
            }

            return 0;
        }
    }
}