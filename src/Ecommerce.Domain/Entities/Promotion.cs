using System;

namespace Ecommerce.Domain.Entities
{
    public class Promotion
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // buy_x_get_y, bundle, tiered_discount, free_gift
        public string RulesJson { get; set; } = string.Empty; // JSON rules for the promotion
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; } // higher priority runs first
        public bool AllowCombine { get; set; } // can combine with other promotions
        public string? ApplicableProductIds { get; set; } // JSON array
        public string? ApplicableCategoryIds { get; set; } // JSON array
        public string? ApplicableUserIds { get; set; } // JSON array
        public string? ExcludedProductIds { get; set; }
        public string? ExcludedCategoryIds { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation properties
        public ICollection<PromotionUsage> Usages { get; set; } = new List<PromotionUsage>();
    }

    public class PromotionUsage
    {
        public Guid Id { get; set; }
        public Guid PromotionId { get; set; }
        public Guid UserId { get; set; }
        public Guid OrderId { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Promotion Promotion { get; set; } = null!;
    }
}
