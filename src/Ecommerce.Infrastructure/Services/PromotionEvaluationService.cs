using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Ecommerce.Infrastructure.Services
{
    public class PromotionEvaluationService : IPromotionEvaluationService
    {
        private readonly IApplicationDbContext _db;
        private readonly IMemoryCache? _cache;
        private static readonly string CacheKey = "active_promotions";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        public PromotionEvaluationService(IApplicationDbContext db, IMemoryCache? cache = null)
        {
            _db = db;
            _cache = cache;
        }

        public void ClearCache()
        {
            _cache?.Remove(CacheKey);
        }

        public async Task<ProductPromotionEvaluation> EvaluateProductAsync(
            Guid productId,
            Guid? categoryId,
            decimal basePrice,
            CancellationToken cancellationToken = default)
        {
            return await EvaluateProductAsync(productId, categoryId, basePrice, 1, cancellationToken);
        }

        public async Task<ProductPromotionEvaluation> EvaluateProductAsync(
            Guid productId,
            Guid? categoryId,
            decimal basePrice,
            int quantity,
            CancellationToken cancellationToken = default)
        {
            var target = new ProductPromotionTarget
            {
                ProductId = productId,
                CategoryId = categoryId,
                BasePrice = basePrice,
                Quantity = quantity
            };

            var dict = await EvaluateProductsAsync(new[] { target }, cancellationToken);
            return dict.TryGetValue(productId, out var eval) ? eval : CreateDefaultEvaluation(productId, basePrice);
        }

        public async Task<Dictionary<Guid, ProductPromotionEvaluation>> EvaluateProductsAsync(
            IEnumerable<ProductPromotionTarget> targets,
            CancellationToken cancellationToken = default)
        {
            var targetList = targets?.ToList() ?? new List<ProductPromotionTarget>();
            var results = new Dictionary<Guid, ProductPromotionEvaluation>();

            foreach (var t in targetList)
            {
                if (!results.ContainsKey(t.ProductId))
                {
                    results[t.ProductId] = CreateDefaultEvaluation(t.ProductId, t.BasePrice);
                }
            }

            if (targetList.Count == 0)
                return results;

            var activePromotions = await GetActivePromotionsAsync(cancellationToken);
            if (activePromotions.Count == 0)
                return results;

            foreach (var target in targetList)
            {
                foreach (var promo in activePromotions)
                {
                    if (!IsPromotionApplicableToProduct(promo, target.ProductId, target.CategoryId))
                        continue;

                    var (hasDiscount, promoPrice, unitDiscount, totalDiscount, discountPercent, badge) =
                        CalculateDiscount(promo, target.BasePrice, target.Quantity);

                    if (hasDiscount && totalDiscount > 0)
                    {
                        results[target.ProductId] = new ProductPromotionEvaluation
                        {
                            ProductId = target.ProductId,
                            BasePrice = target.BasePrice,
                            PromotionalPrice = promoPrice,
                            DiscountAmount = unitDiscount,
                            TotalDiscount = totalDiscount,
                            DiscountPercentage = discountPercent,
                            HasActivePromotion = true,
                            PromotionName = promo.Name,
                            PromotionBadge = badge ?? (discountPercent > 0 ? $"خصم {discountPercent}%" : $"وفر {unitDiscount:G29} ₪"),
                            PromotionId = promo.Id
                        };
                        break; // Highest priority matching promotion takes effect
                    }
                    else if (!string.IsNullOrWhiteSpace(badge))
                    {
                        results[target.ProductId] = new ProductPromotionEvaluation
                        {
                            ProductId = target.ProductId,
                            BasePrice = target.BasePrice,
                            PromotionalPrice = target.BasePrice,
                            DiscountAmount = 0,
                            TotalDiscount = 0,
                            DiscountPercentage = 0,
                            HasActivePromotion = true,
                            PromotionName = promo.Name,
                            PromotionBadge = badge,
                            PromotionId = promo.Id
                        };
                        break;
                    }
                }
            }

            return results;
        }

        private async Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            List<Promotion> activePromotions;
            if (_cache != null)
            {
                activePromotions = await _cache.GetOrCreateAsync(CacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                    return await _db.Promotions
                        .AsNoTracking()
                        .Where(p => p.IsActive &&
                                   (!p.StartAt.HasValue || p.StartAt.Value <= now) &&
                                   (!p.EndAt.HasValue || p.EndAt.Value >= now) &&
                                   (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value))
                        .OrderByDescending(p => p.Priority)
                        .ThenByDescending(p => p.CreatedAt)
                        .ToListAsync(cancellationToken);
                }) ?? new List<Promotion>();
            }
            else
            {
                activePromotions = await _db.Promotions
                    .AsNoTracking()
                    .Where(p => p.IsActive &&
                               (!p.StartAt.HasValue || p.StartAt.Value <= now) &&
                               (!p.EndAt.HasValue || p.EndAt.Value >= now) &&
                               (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value))
                    .OrderByDescending(p => p.Priority)
                    .ThenByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            
            return activePromotions;
        }

        public async Task<CartLevelPromotionResult> EvaluateCartLevelPromotionsAsync(List<CartLevelPromotionTarget> cartItems, decimal currentSubtotal, CancellationToken cancellationToken = default)
        {
            var promotions = await GetActivePromotionsAsync(cancellationToken);
            var cartPromos = promotions.Where(p => 
                p.Type == "bundle" || 
                p.Type == "tiered_discount" || 
                p.Type == "free_gift").ToList();

            var result = new CartLevelPromotionResult
            {
                HasCartLevelPromotion = false,
                TotalCartDiscount = 0,
                PromotionName = null,
                PromotionId = null,
                SuggestedFreeGiftProductId = null
            };

            foreach (var promo in cartPromos.OrderByDescending(p => p.Priority))
            {
                var rules = ParseRules(promo.RulesJson);
                var type = promo.Type?.ToLowerInvariant().Trim();

                if (type == "tiered_discount")
                {
                    if (rules.TryGetValue("tiers", out var tiers) && tiers.ValueKind == JsonValueKind.Array)
                    {
                        decimal eligibleSubtotal = 0m;
                        foreach (var ci in cartItems)
                        {
                            if (IsPromotionApplicableToProduct(promo, ci.ProductId, ci.CategoryId))
                            {
                                eligibleSubtotal += (ci.UnitPrice * ci.Quantity);
                            }
                        }

                        decimal bestDiscount = 0;
                        foreach (var tier in tiers.EnumerateArray())
                        {
                            if (tier.TryGetProperty("minSpend", out var ms) && tier.TryGetProperty("discount", out var d))
                            {
                                decimal minSpend = ms.GetDecimal();
                                decimal discountVal = d.GetDecimal();
                                if (eligibleSubtotal >= minSpend)
                                {
                                    decimal calculatedDiscount = discountVal <= 100m ? Math.Round(eligibleSubtotal * (discountVal / 100m), 2) : discountVal;
                                    if (calculatedDiscount > bestDiscount)
                                        bestDiscount = calculatedDiscount;
                                }
                            }
                        }

                        if (bestDiscount > 0)
                        {
                            result.HasCartLevelPromotion = true;
                            result.TotalCartDiscount = bestDiscount;
                            result.PromotionName = promo.Name;
                            result.PromotionId = promo.Id;
                            break;
                        }
                    }
                }
                else if (type == "bundle")
                {
                    if (rules.TryGetValue("bundlePrice", out var bp) && bp.ValueKind == JsonValueKind.Number)
                    {
                        decimal bundlePrice = bp.GetDecimal();
                        // Wait, a bundle usually requires specific product IDs to be present.
                        var reqIds = ParseGuidList(promo.ApplicableProductIds ?? "");
                        if (reqIds.Count > 0)
                        {
                            bool hasAll = reqIds.All(id => cartItems.Any(ci => ci.ProductId == id));
                            if (hasAll)
                            {
                                decimal sumOfBundleItems = cartItems.Where(ci => reqIds.Contains(ci.ProductId)).Sum(ci => ci.UnitPrice); // Just 1 of each for the bundle
                                if (sumOfBundleItems > bundlePrice)
                                {
                                    result.HasCartLevelPromotion = true;
                                    result.TotalCartDiscount = sumOfBundleItems - bundlePrice;
                                    result.PromotionName = promo.Name;
                                    result.PromotionId = promo.Id;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (type == "free_gift")
                {
                    decimal minSpend = 0;
                    if (rules.TryGetValue("minSpend", out var ms) && ms.ValueKind == JsonValueKind.Number)
                        minSpend = ms.GetDecimal();

                    string giftIdStr = string.Empty;
                    if (rules.TryGetValue("giftProductId", out var gid))
                        giftIdStr = gid.GetString() ?? "";

                    decimal eligibleSubtotal = 0m;
                    foreach (var ci in cartItems)
                    {
                        if (IsPromotionApplicableToProduct(promo, ci.ProductId, ci.CategoryId))
                        {
                            eligibleSubtotal += (ci.UnitPrice * ci.Quantity);
                        }
                    }

                    if (eligibleSubtotal >= minSpend && Guid.TryParse(giftIdStr, out var giftProductId))
                    {
                        var giftItem = cartItems.FirstOrDefault(ci => ci.ProductId == giftProductId);
                        if (giftItem != null)
                        {
                            result.HasCartLevelPromotion = true;
                            result.TotalCartDiscount = giftItem.UnitPrice; // 1 free gift
                            result.PromotionName = promo.Name;
                            result.PromotionId = promo.Id;
                            break;
                        }
                        else
                        {
                            // Eligible but not in cart
                            result.SuggestedFreeGiftProductId = giftProductId;
                        }
                    }
                }
            }

            return result;
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

        private static (bool hasDiscount, decimal promoPrice, decimal unitDiscount, decimal totalDiscount, int discountPercent, string? badge)
            CalculateDiscount(Promotion promo, decimal basePrice, int quantity = 1)
        {
            if (basePrice <= 0)
                return (false, basePrice, 0, 0, 0, null);

            var type = promo.Type?.ToLowerInvariant().Trim() ?? "percentage";
            var rules = ParseRules(promo.RulesJson);
            int qty = Math.Max(1, quantity);

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

                if (percentage > 0)
                {
                    percentage = Math.Min(100m, Math.Max(0m, percentage));
                    var unitDiscount = Math.Round(basePrice * (percentage / 100m), 2);
                    var promoPrice = Math.Max(0m, basePrice - unitDiscount);
                    var discountPercent = (int)Math.Round(percentage);
                    var totalDiscount = unitDiscount * qty;
                    return (true, promoPrice, unitDiscount, totalDiscount, discountPercent, $"خصم {discountPercent}%");
                }
            }
            else if (type is "fixed_amount" or "fixed_discount")
            {
                if (rules.TryGetValue("discountAmount", out var da) && da.ValueKind == JsonValueKind.Number)
                    fixedAmount = da.GetDecimal();
                else if (rules.TryGetValue("amount", out var a) && a.ValueKind == JsonValueKind.Number)
                    fixedAmount = a.GetDecimal();
                else if (rules.TryGetValue("value", out var v) && v.ValueKind == JsonValueKind.Number)
                    fixedAmount = v.GetDecimal();

                if (fixedAmount > 0)
                {
                    var unitDiscount = Math.Min(basePrice, fixedAmount);
                    var promoPrice = Math.Max(0m, basePrice - unitDiscount);
                    var discountPercent = basePrice > 0 ? (int)Math.Round((unitDiscount / basePrice) * 100m) : 0;
                    var totalDiscount = unitDiscount * qty;
                    return (true, promoPrice, unitDiscount, totalDiscount, discountPercent, $"وفر {unitDiscount:G29} ₪");
                }
            }
            else if (type is "buy_x_get_y")
            {
                int buyQty = 2;
                if (rules.TryGetValue("buyQuantity", out var bq) && bq.ValueKind == JsonValueKind.Number)
                    buyQty = bq.GetInt32();
                else if (rules.TryGetValue("buy_quantity", out var bq2) && bq2.ValueKind == JsonValueKind.Number)
                    buyQty = bq2.GetInt32();
                else if (rules.TryGetValue("buy", out var bq3) && bq3.ValueKind == JsonValueKind.Number)
                    buyQty = bq3.GetInt32();

                int getQty = 1;
                if (rules.TryGetValue("getQuantity", out var gq) && gq.ValueKind == JsonValueKind.Number)
                    getQty = gq.GetInt32();
                else if (rules.TryGetValue("get_quantity", out var gq2) && gq2.ValueKind == JsonValueKind.Number)
                    getQty = gq2.GetInt32();
                else if (rules.TryGetValue("get", out var gq3) && gq3.ValueKind == JsonValueKind.Number)
                    getQty = gq3.GetInt32();

                decimal getDiscountPercent = 100m;
                if (rules.TryGetValue("discountPercentage", out var dp) && dp.ValueKind == JsonValueKind.Number)
                    getDiscountPercent = dp.GetDecimal();
                else if (rules.TryGetValue("discount_percentage", out var dp2) && dp2.ValueKind == JsonValueKind.Number)
                    getDiscountPercent = dp2.GetDecimal();
                else if (rules.TryGetValue("percentage", out var dp3) && dp3.ValueKind == JsonValueKind.Number)
                    getDiscountPercent = dp3.GetDecimal();

                string badgeText = getDiscountPercent >= 100m
                    ? $"اشتر {buyQty} واحصل على {getQty} مجاناً"
                    : $"اشتر {buyQty} واحصل على {getQty} بخصم {getDiscountPercent:0}%";
                
                if (!string.IsNullOrWhiteSpace(promo.Name) && 
                    !promo.Name.Contains("اشتر") && 
                    !promo.Name.Contains("Buy") &&
                    promo.Name.Trim() != badgeText.Trim())
                {
                    badgeText = $"{badgeText} - {promo.Name}";
                }

                int bundleSize = buyQty + getQty;
                if (bundleSize > 0 && qty >= bundleSize)
                {
                    int sets = qty / bundleSize;
                    int freeItemCount = sets * getQty;
                    decimal discountPerFreeItem = Math.Round(basePrice * (getDiscountPercent / 100m), 2);
                    decimal totalDiscount = freeItemCount * discountPerFreeItem;
                    decimal effectiveTotal = Math.Max(0m, (qty * basePrice) - totalDiscount);
                    decimal effectiveUnitPrice = Math.Round(effectiveTotal / qty, 2);
                    int effectiveDiscountPercent = (int)Math.Round((totalDiscount / (qty * basePrice)) * 100m);

                    return (true, effectiveUnitPrice, Math.Round(totalDiscount / qty, 2), totalDiscount, effectiveDiscountPercent, badgeText);
                }

                return (false, basePrice, 0, 0, 0, badgeText);
            }
            else if (type is "bundle" or "tiered_discount" or "free_gift")
            {
                return (false, basePrice, 0, 0, 0, promo.Name);
            }

            return (false, basePrice, 0, 0, 0, null);
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
