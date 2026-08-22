using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services
{
    public class PromotionEvaluationService : IPromotionEvaluationService
    {
        private readonly IApplicationDbContext _db;

        public PromotionEvaluationService(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ProductPromotionEvaluation> EvaluateProductAsync(
            Guid productId,
            Guid? categoryId,
            decimal basePrice,
            CancellationToken cancellationToken = default)
        {
            var target = new ProductPromotionTarget
            {
                ProductId = productId,
                CategoryId = categoryId,
                BasePrice = basePrice
            };

            var dict = await EvaluateProductsAsync(new[] { target }, cancellationToken);
            return dict.TryGetValue(productId, out var eval) ? eval : CreateDefaultEvaluation(productId, basePrice);
        }

        public async Task<Dictionary<Guid, ProductPromotionEvaluation>> EvaluateProductsAsync(
            IEnumerable<ProductPromotionTarget> targets,
            CancellationToken cancellationToken = default)
        {
            var targetList = targets?.ToList() ?? new List<ProductPromotionTarget>();
            var results = targetList.ToDictionary(
                t => t.ProductId,
                t => CreateDefaultEvaluation(t.ProductId, t.BasePrice));

            if (targetList.Count == 0)
                return results;

            var now = DateTimeOffset.UtcNow;

            var activePromotions = await _db.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive &&
                           (!p.StartAt.HasValue || p.StartAt.Value <= now) &&
                           (!p.EndAt.HasValue || p.EndAt.Value >= now) &&
                           (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value))
                .OrderByDescending(p => p.Priority)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            if (activePromotions.Count == 0)
                return results;

            foreach (var target in targetList)
            {
                foreach (var promo in activePromotions)
                {
                    if (!IsPromotionApplicableToProduct(promo, target.ProductId, target.CategoryId))
                        continue;

                    var (hasDiscount, promoPrice, discountAmount, discountPercent, badge) =
                        CalculateDiscount(promo, target.BasePrice);

                    if (hasDiscount && discountAmount > 0)
                    {
                        results[target.ProductId] = new ProductPromotionEvaluation
                        {
                            ProductId = target.ProductId,
                            BasePrice = target.BasePrice,
                            PromotionalPrice = promoPrice,
                            DiscountAmount = discountAmount,
                            DiscountPercentage = discountPercent,
                            HasActivePromotion = true,
                            PromotionName = promo.Name,
                            PromotionBadge = badge ?? (discountPercent > 0 ? $"خصم {discountPercent}%" : $"وفر {discountAmount:G29} ₪"),
                            PromotionId = promo.Id
                        };
                        break; // Highest priority matching promotion takes effect
                    }
                }
            }

            return results;
        }

        private static bool IsPromotionApplicableToProduct(Promotion promo, Guid productId, Guid? categoryId)
        {
            // 1. Check exclusions first
            if (!string.IsNullOrWhiteSpace(promo.ExcludedProductIds))
            {
                var excludedIds = ParseGuidList(promo.ExcludedProductIds);
                if (excludedIds.Contains(productId))
                    return false;
            }

            if (categoryId.HasValue && !string.IsNullOrWhiteSpace(promo.ExcludedCategoryIds))
            {
                var excludedCatIds = ParseGuidList(promo.ExcludedCategoryIds);
                if (excludedCatIds.Contains(categoryId.Value))
                    return false;
            }

            // 2. Check inclusions (applicable products / categories)
            bool hasProductFilter = !string.IsNullOrWhiteSpace(promo.ApplicableProductIds);
            bool hasCategoryFilter = !string.IsNullOrWhiteSpace(promo.ApplicableCategoryIds);

            if (!hasProductFilter && !hasCategoryFilter)
            {
                // Store-wide promotion
                return true;
            }

            if (hasProductFilter)
            {
                var applicableProductIds = ParseGuidList(promo.ApplicableProductIds!);
                if (applicableProductIds.Contains(productId))
                    return true;
            }

            if (hasCategoryFilter && categoryId.HasValue)
            {
                var applicableCatIds = ParseGuidList(promo.ApplicableCategoryIds!);
                if (applicableCatIds.Contains(categoryId.Value))
                    return true;
            }

            return false;
        }

        private static (bool hasDiscount, decimal promoPrice, decimal discountAmount, int discountPercent, string? badge)
            CalculateDiscount(Promotion promo, decimal basePrice)
        {
            if (basePrice <= 0)
                return (false, basePrice, 0, 0, null);

            var type = promo.Type?.ToLowerInvariant().Trim() ?? "percentage";
            var rules = ParseRules(promo.RulesJson);

            decimal percentage = 0;
            decimal fixedAmount = 0;

            if (type is "percentage" or "percentage_discount")
            {
                if (rules.TryGetValue("discountPercentage", out var dp) && dp.ValueKind == JsonValueKind.Number)
                    percentage = dp.GetDecimal();
                else if (rules.TryGetValue("percentage", out var p) && p.ValueKind == JsonValueKind.Number)
                    percentage = p.GetDecimal();
                else if (rules.TryGetValue("value", out var v) && v.ValueKind == JsonValueKind.Number)
                    percentage = v.GetDecimal();
            }
            else if (type is "fixed_amount" or "fixed_discount")
            {
                if (rules.TryGetValue("discountAmount", out var da) && da.ValueKind == JsonValueKind.Number)
                    fixedAmount = da.GetDecimal();
                else if (rules.TryGetValue("amount", out var a) && a.ValueKind == JsonValueKind.Number)
                    fixedAmount = a.GetDecimal();
                else if (rules.TryGetValue("value", out var v) && v.ValueKind == JsonValueKind.Number)
                    fixedAmount = v.GetDecimal();
            }
            else if (type is "buy_x_get_y" or "bundle" or "tiered_discount" or "free_gift")
            {
                if (rules.TryGetValue("discountPercentage", out var dp) && dp.ValueKind == JsonValueKind.Number)
                    percentage = dp.GetDecimal();
                else
                    return (true, basePrice, 0, 0, promo.Name);
            }

            if (percentage > 0)
            {
                percentage = Math.Min(100m, Math.Max(0m, percentage));
                var discountAmount = Math.Round(basePrice * (percentage / 100m), 2);
                var promoPrice = Math.Max(0m, basePrice - discountAmount);
                var discountPercent = (int)Math.Round(percentage);
                return (true, promoPrice, discountAmount, discountPercent, $"خصم {discountPercent}%");
            }

            if (fixedAmount > 0)
            {
                var discountAmount = Math.Min(basePrice, fixedAmount);
                var promoPrice = Math.Max(0m, basePrice - discountAmount);
                var discountPercent = basePrice > 0 ? (int)Math.Round((discountAmount / basePrice) * 100m) : 0;
                return (true, promoPrice, discountAmount, discountPercent, $"وفر {discountAmount:G29} ₪");
            }

            return (false, basePrice, 0, 0, null);
        }

        private static Dictionary<string, JsonElement> ParseRules(string? rulesJson)
        {
            if (string.IsNullOrWhiteSpace(rulesJson))
                return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    rulesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static HashSet<Guid> ParseGuidList(string jsonOrCsv)
        {
            var set = new HashSet<Guid>();
            if (string.IsNullOrWhiteSpace(jsonOrCsv))
                return set;

            var trimmed = jsonOrCsv.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    var guids = JsonSerializer.Deserialize<List<string>>(trimmed);
                    if (guids != null)
                    {
                        foreach (var g in guids)
                        {
                            if (Guid.TryParse(g, out var parsed))
                                set.Add(parsed);
                        }
                    }
                    return set;
                }
                catch
                {
                    // Fallback to split
                }
            }

            var parts = trimmed.Split(new[] { ',', ';', '[', ']', '"', '\'', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Guid.TryParse(part, out var parsed))
                    set.Add(parsed);
            }

            return set;
        }

        private static ProductPromotionEvaluation CreateDefaultEvaluation(Guid productId, decimal basePrice)
        {
            return new ProductPromotionEvaluation
            {
                ProductId = productId,
                BasePrice = basePrice,
                PromotionalPrice = basePrice,
                DiscountAmount = 0,
                DiscountPercentage = 0,
                HasActivePromotion = false,
                PromotionName = null,
                PromotionBadge = null,
                PromotionId = null
            };
        }
    }
}
