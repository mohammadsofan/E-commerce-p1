using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreatePromotionCommand : ICommand<AdminPromotionDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // buy_x_get_y, bundle, tiered_discount, free_gift
        public string RulesJson { get; set; } = string.Empty;
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0;
        public bool AllowCombine { get; set; } = false;
        public string? ApplicableProductIds { get; set; }
        public string? ApplicableCategoryIds { get; set; }
        public string? ApplicableUserIds { get; set; }
        public string? ExcludedProductIds { get; set; }
        public string? ExcludedCategoryIds { get; set; }
        public int? UsageLimit { get; set; }
    }

    public class UpdatePromotionCommand : ICommand<AdminPromotionDto>
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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class DeletePromotionCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}