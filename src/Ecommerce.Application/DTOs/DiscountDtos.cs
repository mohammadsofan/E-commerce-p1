using System;

namespace Ecommerce.Application.DTOs
{
    public class AdminCouponDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int? PerUserLimit { get; set; }
        public bool IsActive { get; set; }
        public bool AllowCombine { get; set; }
        public string? ApplicableProductIds { get; set; }
        public string? ApplicableCategoryIds { get; set; }
        public string? ApplicableUserIds { get; set; }
        public string? ExcludedProductIds { get; set; }
        public string? ExcludedCategoryIds { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class AdminPromotionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string RulesJson { get; set; } = string.Empty;
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public bool AllowCombine { get; set; }
        public string? ApplicableProductIds { get; set; }
        public string? ApplicableCategoryIds { get; set; }
        public string? ApplicableUserIds { get; set; }
        public string? ExcludedProductIds { get; set; }
        public string? ExcludedCategoryIds { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class DiscountCalculationResult
    {
        public decimal Subtotal { get; set; }
        public decimal CouponDiscount { get; set; }
        public decimal PromotionDiscount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal FinalTotal { get; set; }
        public List<AppliedDiscount> AppliedDiscounts { get; set; } = new();
    }

    public class AppliedDiscount
    {
        public string Type { get; set; } = string.Empty; // coupon, promotion
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ValidateCouponRequest
    {
        public string Code { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public decimal OrderTotal { get; set; }
        public List<Guid> ProductIds { get; set; } = new();
        public List<Guid> CategoryIds { get; set; } = new();
    }

    public class ValidateCouponResponse
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public AdminCouponDto? Coupon { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}