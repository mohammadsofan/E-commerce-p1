using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateCouponCommand : ICommand<AdminCouponDto>
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "percentage"; // percentage, fixed_amount, free_shipping
        public decimal Value { get; set; }
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int? PerUserLimit { get; set; }
        public bool IsActive { get; set; } = true;
        public bool AllowCombine { get; set; } = false;
        public string? ApplicableProductIds { get; set; }
        public string? ApplicableCategoryIds { get; set; }
        public string? ApplicableUserIds { get; set; }
        public string? ExcludedProductIds { get; set; }
        public string? ExcludedCategoryIds { get; set; }
    }

    public class UpdateCouponCommand : ICommand<AdminCouponDto>
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
        public int? PerUserLimit { get; set; }
        public bool IsActive { get; set; }
        public bool AllowCombine { get; set; }
        public string? ApplicableProductIds { get; set; }
        public string? ApplicableCategoryIds { get; set; }
        public string? ApplicableUserIds { get; set; }
        public string? ExcludedProductIds { get; set; }
        public string? ExcludedCategoryIds { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class DeleteCouponCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}