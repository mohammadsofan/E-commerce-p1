using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Common.Discounts
{
    /// <summary>
    /// A single cart/order line as seen by the coupon engine. <see cref="LineTotal"/> is the
    /// amount actually payable for the line *after* item-level promotions, because a coupon
    /// may never discount more than what the customer is being charged.
    /// </summary>
    public sealed class CouponLine
    {
        public Guid ProductId { get; init; }
        public Guid? CategoryId { get; init; }
        public decimal LineTotal { get; init; }
    }

    /// <summary>
    /// The outcome of evaluating a coupon against a concrete set of lines.
    /// </summary>
    public sealed class CouponDiscountResult
    {
        /// <summary>False when the coupon is scoped and no line in the cart is in scope.</summary>
        public bool IsApplicable { get; init; }

        /// <summary>Customer-facing reason when <see cref="IsApplicable"/> is false.</summary>
        public string? RejectionReason { get; init; }

        /// <summary>Sum of the line totals the coupon is allowed to touch.</summary>
        public decimal EligibleSubtotal { get; init; }

        /// <summary>Discount to charge, already capped and clamped. Never negative.</summary>
        public decimal DiscountAmount { get; init; }

        public bool IsFreeShipping { get; init; }
    }

    /// <summary>
    /// The single coupon-eligibility and coupon-arithmetic authority.
    ///
    /// Cart application (<c>ApplyCouponToCartCommandHandler</c> / <c>CartAccessor</c>),
    /// checkout (<c>CheckoutCommandHandler</c>) and the advisory query handlers
    /// (<c>ValidateCouponQueryHandler</c> / <c>CalculateDiscountsQueryHandler</c>) all route
    /// through here. Before this existed each of the three had its own copy of the rules, so:
    ///
    /// * product/category scoping (D-02) was only honoured by the advisory query — cart and
    ///   checkout discounted products the coupon explicitly excluded;
    /// * <see cref="Coupon.MaxDiscountAmount"/> (D-21) was only applied to percentage coupons in
    ///   cart/checkout, so a <c>fixed_amount 40 / cap 5</c> coupon gave 5 from the query and 40
    ///   from the order;
    /// * nothing bounded the result by the line total (D-07), so an oversized rule could drive a
    ///   line — and the order — below zero.
    ///
    /// All three rules now live in one place and cannot drift again.
    /// </summary>
    public static class CouponDiscountCalculator
    {
        public const string IneligibleProductsMessage = "الكوبون غير صالح للمنتجات الموجودة في سلتك";

        /// <summary>
        /// True when a coupon may discount the given product. Exclusions always win over
        /// inclusions; a coupon with no scoping at all is store-wide.
        /// </summary>
        public static bool IsLineEligible(Coupon coupon, Guid productId, Guid? categoryId)
        {
            if (coupon == null) return false;

            // 1. Exclusions take precedence.
            if (!string.IsNullOrWhiteSpace(coupon.ExcludedProductIds) &&
                ParseGuidList(coupon.ExcludedProductIds).Contains(productId))
            {
                return false;
            }

            if (categoryId.HasValue &&
                !string.IsNullOrWhiteSpace(coupon.ExcludedCategoryIds) &&
                ParseGuidList(coupon.ExcludedCategoryIds).Contains(categoryId.Value))
            {
                return false;
            }

            // 2. Inclusions. A product qualifies if it matches the product filter or the
            //    category filter; with no filter configured the coupon is store-wide.
            var hasProductFilter = !string.IsNullOrWhiteSpace(coupon.ApplicableProductIds);
            var hasCategoryFilter = !string.IsNullOrWhiteSpace(coupon.ApplicableCategoryIds);

            if (!hasProductFilter && !hasCategoryFilter) return true;

            if (hasProductFilter && ParseGuidList(coupon.ApplicableProductIds!).Contains(productId))
                return true;

            if (hasCategoryFilter && categoryId.HasValue &&
                ParseGuidList(coupon.ApplicableCategoryIds!).Contains(categoryId.Value))
            {
                return true;
            }

            return false;
        }

        /// <summary>True when any of the four scoping fields is configured.</summary>
        public static bool HasScoping(Coupon coupon)
        {
            return coupon != null &&
                   (!string.IsNullOrWhiteSpace(coupon.ApplicableProductIds) ||
                    !string.IsNullOrWhiteSpace(coupon.ApplicableCategoryIds) ||
                    !string.IsNullOrWhiteSpace(coupon.ExcludedProductIds) ||
                    !string.IsNullOrWhiteSpace(coupon.ExcludedCategoryIds));
        }

        /// <summary>
        /// Evaluates the coupon against the supplied lines.
        /// </summary>
        /// <param name="cartLevelDiscount">
        /// Cart-level promotion discount already granted. The coupon can only work on what is
        /// still payable, so it lowers the ceiling for the coupon discount.
        /// </param>
        public static CouponDiscountResult Calculate(
            Coupon coupon,
            IEnumerable<CouponLine> lines,
            decimal cartLevelDiscount = 0m)
        {
            var lineList = lines?.ToList() ?? new List<CouponLine>();
            var grossSubtotal = lineList.Sum(l => Math.Max(0m, l.LineTotal));

            // What is still payable after cart-level promotions: the coupon can never exceed it.
            var payableSubtotal = Math.Max(0m, grossSubtotal - Math.Max(0m, cartLevelDiscount));

            var eligibleSubtotal = lineList
                .Where(l => IsLineEligible(coupon, l.ProductId, l.CategoryId))
                .Sum(l => Math.Max(0m, l.LineTotal));

            if (HasScoping(coupon) && eligibleSubtotal <= 0m)
            {
                return new CouponDiscountResult
                {
                    IsApplicable = false,
                    RejectionReason = IneligibleProductsMessage,
                    EligibleSubtotal = 0m,
                    DiscountAmount = 0m,
                    IsFreeShipping = false
                };
            }

            // The base is bounded by both the eligible lines and what is still payable.
            var discountBase = Math.Min(eligibleSubtotal, payableSubtotal);

            return new CouponDiscountResult
            {
                IsApplicable = true,
                RejectionReason = null,
                EligibleSubtotal = eligibleSubtotal,
                DiscountAmount = CalculateAmount(coupon, discountBase),
                IsFreeShipping = IsFreeShippingCoupon(coupon)
            };
        }

        /// <summary>
        /// Coupon arithmetic for callers that already know the eligible base (for example the
        /// advisory validate endpoint, which is handed an order total rather than lines).
        ///
        /// <see cref="Coupon.MaxDiscountAmount"/> is honoured for every coupon type, and the
        /// result can never exceed <paramref name="eligibleBase"/> nor go below zero.
        /// </summary>
        public static decimal CalculateAmount(Coupon coupon, decimal eligibleBase)
        {
            if (coupon == null) return 0m;

            var basis = Math.Max(0m, eligibleBase);
            if (basis <= 0m) return 0m;

            var type = (coupon.Type ?? string.Empty).Trim().ToLowerInvariant();

            decimal discount;
            if (type == "percentage")
            {
                // A malformed/over-100 percentage can never give the product away.
                var percentage = Math.Clamp(coupon.Value, 0m, 100m);
                discount = Math.Round(basis * (percentage / 100m), 2, MidpointRounding.AwayFromZero);
            }
            else if (type == "free_shipping")
            {
                // Shipping is zeroed by the caller; there is no line discount.
                return 0m;
            }
            else
            {
                // fixed_amount and any unknown type behave as a flat amount.
                discount = Math.Max(0m, coupon.Value);
            }

            // D-21: the cap applies to fixed_amount coupons too, not just percentages.
            if (coupon.MaxDiscountAmount.HasValue && coupon.MaxDiscountAmount.Value > 0m)
            {
                discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
            }

            // D-07: never discount more than the eligible amount, never a negative discount.
            return Math.Clamp(discount, 0m, basis);
        }

        public static bool IsFreeShippingCoupon(Coupon coupon)
        {
            return string.Equals((coupon?.Type ?? string.Empty).Trim(), "free_shipping", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses the persisted scoping columns. They are written as a JSON array by the admin
        /// API but historic rows contain comma-separated ids, so both are accepted.
        /// </summary>
        public static HashSet<Guid> ParseGuidList(string? jsonOrCsv)
        {
            var set = new HashSet<Guid>();
            if (string.IsNullOrWhiteSpace(jsonOrCsv)) return set;

            var trimmed = jsonOrCsv.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
            {
                try
                {
                    var values = JsonSerializer.Deserialize<List<string>>(trimmed);
                    if (values != null)
                    {
                        foreach (var value in values)
                        {
                            if (Guid.TryParse(value, out var parsed)) set.Add(parsed);
                        }

                        return set;
                    }
                }
                catch (JsonException)
                {
                    // Malformed JSON: fall through to the permissive split below.
                }
            }

            var parts = trimmed.Split(new[] { ',', ';', '[', ']', '"', '\'', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Guid.TryParse(part, out var parsed)) set.Add(parsed);
            }

            return set;
        }
    }
}
