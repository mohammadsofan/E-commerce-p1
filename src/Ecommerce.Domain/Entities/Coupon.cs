using System;

namespace Ecommerce.Domain.Entities
{
    public class Coupon
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // percentage, fixed_amount, free_shipping
        public decimal Value { get; set; } // percentage (e.g., 10 for 10%) or fixed amount
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int? PerUserLimit { get; set; }
        public bool IsActive { get; set; }
        public bool AllowCombine { get; set; } // can combine with other coupons
        public string? ApplicableProductIds { get; set; } // JSON array of product IDs
        public string? ApplicableCategoryIds { get; set; } // JSON array of category IDs
        public string? ApplicableUserIds { get; set; } // JSON array of user IDs (for targeted coupons)
        public string? ExcludedProductIds { get; set; } // JSON array of product IDs
        public string? ExcludedCategoryIds { get; set; } // JSON array of category IDs
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation properties
        public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
    }

    public class CouponUsage
    {
        public Guid Id { get; set; }
        public Guid CouponId { get; set; }
        public Guid UserId { get; set; }
        public Guid OrderId { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Coupon Coupon { get; set; } = null!;
    }
}
