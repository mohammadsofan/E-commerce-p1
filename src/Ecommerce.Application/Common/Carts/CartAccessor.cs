using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Common.Carts
{
    /// <summary>
    /// Shared cart access logic for command and query handlers: resolves the current
    /// user's active cart (get-or-create) or throws if a cart is required but missing.
    /// </summary>
    public abstract class CartAccessor
    {
        protected readonly IApplicationDbContext Db;
        protected readonly ICurrentUserService CurrentUser;
        protected readonly IMapper Mapper;
        protected readonly IPromotionEvaluationService? PromotionEvaluator;

        protected CartAccessor(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IMapper mapper,
            IPromotionEvaluationService? promotionEvaluator = null)
        {
            Db = db;
            CurrentUser = currentUser;
            Mapper = mapper;
            PromotionEvaluator = promotionEvaluator;
        }

        protected async Task<Cart> GetOrCreateCartAsync(CancellationToken cancellationToken)
        {
            var userId = CurrentUser.UserId ?? throw new DomainException("User is not authenticated");

            var cart = await Db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, cancellationToken);

            if (cart == null)
            {
                cart = Cart.Create(userId, null);
                await Db.Carts.AddAsync(cart, cancellationToken);
            }

            return cart;
        }

        protected async Task<Cart> GetCartOrThrowAsync(CancellationToken cancellationToken)
        {
            var userId = CurrentUser.UserId ?? throw new DomainException("User is not authenticated");

            var cart = await Db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, cancellationToken);

            return cart ?? throw new NotFoundException("Cart", userId);
        }

        protected async Task<CartDto> MapAsync(Cart cart, CancellationToken cancellationToken)
        {
            var result = Mapper.Map<CartDto>(cart);
            var productIds = result.Items.Select(item => item.ProductId).Distinct().ToList();
            if (productIds.Count == 0) return result;

            var variantIds = result.Items
                .Where(item => item.ProductVariantId.HasValue && item.ProductVariantId.Value != Guid.Empty)
                .Select(item => item.ProductVariantId!.Value)
                .Distinct()
                .ToList();

            var products = await Db.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, product.Name, product.Slug, product.CategoryId, product.BasePrice })
                .ToListAsync(cancellationToken);

            var variants = variantIds.Any()
                ? await Db.ProductVariants
                    .AsNoTracking()
                    .Where(v => variantIds.Contains(v.Id))
                    .Select(v => new { v.Id, v.Name, v.Price, v.Sku })
                    .ToListAsync(cancellationToken)
                : null;

            var images = await Db.ProductImages
                .AsNoTracking()
                .Where(image => productIds.Contains(image.ProductId))
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, ProductPromotionEvaluation>? promoEvaluations = null;
            if (PromotionEvaluator != null)
            {
                var targets = result.Items
                    .GroupBy(i => i.ProductId)
                    .Select(g =>
                    {
                        var prod = products.FirstOrDefault(p => p.Id == g.Key);
                        var firstItem = g.First();
                        decimal targetBasePrice = firstItem.UnitPrice;
                        if (firstItem.ProductVariantId.HasValue && variants != null)
                        {
                            var variant = variants.FirstOrDefault(v => v.Id == firstItem.ProductVariantId.Value);
                            if (variant != null) targetBasePrice = variant.Price;
                        }
                        else if (prod != null)
                        {
                            targetBasePrice = prod.BasePrice;
                        }

                        return new ProductPromotionTarget
                        {
                            ProductId = g.Key,
                            CategoryId = prod?.CategoryId,
                            BasePrice = targetBasePrice,
                            Quantity = g.Sum(i => i.Quantity)
                        };
                    })
                    .ToList();

                promoEvaluations = await PromotionEvaluator.EvaluateProductsAsync(targets, cancellationToken);
            }

            foreach (var item in result.Items)
            {
                var prod = products.FirstOrDefault(product => product.Id == item.ProductId);
                if (prod != null)
                {
                    if (string.IsNullOrWhiteSpace(item.ProductName))
                    {
                        item.ProductName = prod.Name;
                    }
                    item.ProductSlug = prod.Slug ?? string.Empty;
                }

                if (item.ProductVariantId.HasValue && variants != null)
                {
                    var variant = variants.FirstOrDefault(v => v.Id == item.ProductVariantId.Value);
                    if (variant != null && !string.IsNullOrWhiteSpace(variant.Name))
                    {
                        item.VariantName = variant.Name;
                    }
                }

                item.ImageUrl = images
                    .Where(image => image.ProductId == item.ProductId && image.ProductVariantId == item.ProductVariantId)
                    .Select(image => image.Url)
                    .FirstOrDefault()
                    ?? images
                        .Where(image => image.ProductId == item.ProductId && image.ProductVariantId == null)
                        .Select(image => image.Url)
                        .FirstOrDefault();

                decimal basePrice = item.UnitPrice;
                if (item.ProductVariantId.HasValue && variants != null)
                {
                    var variant = variants.FirstOrDefault(v => v.Id == item.ProductVariantId.Value);
                    if (variant != null) basePrice = variant.Price;
                }
                else if (prod != null)
                {
                    basePrice = prod.BasePrice;
                }

                item.OriginalPrice = basePrice;
                item.UnitPrice = basePrice;
                item.LineTotal = item.UnitPrice * item.Quantity;

                if (promoEvaluations != null && promoEvaluations.TryGetValue(item.ProductId, out var eval) && eval.HasActivePromotion)
                {
                    if (eval.TotalDiscount > 0)
                    {
                        int totalProductQty = result.Items.Where(i => i.ProductId == item.ProductId).Sum(i => i.Quantity);
                        decimal itemShare = totalProductQty > 0 ? (decimal)item.Quantity / totalProductQty : 1m;
                        decimal itemDiscount = Math.Round(eval.TotalDiscount * itemShare, 2);

                        item.PromotionalPrice = eval.PromotionalPrice;
                        item.LineTotal = Math.Max(0, (item.UnitPrice * item.Quantity) - itemDiscount);
                    }
                    else if (eval.DiscountAmount > 0 && eval.PromotionalPrice < item.OriginalPrice)
                    {
                        item.PromotionalPrice = eval.PromotionalPrice;
                        item.LineTotal = eval.PromotionalPrice * item.Quantity;
                    }

                    item.PromotionName = eval.PromotionName;
                    item.PromotionBadge = eval.PromotionBadge;
                }
            }

            result.Subtotal = result.Items.Sum(i => i.LineTotal);

            if (PromotionEvaluator != null)
            {
                var cartTargets = result.Items.Select(i => new CartLevelPromotionTarget
                {
                    ProductId = i.ProductId,
                    UnitPrice = i.Quantity > 0 ? (i.LineTotal / i.Quantity) : i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList();

                var cartLevelEval = await PromotionEvaluator.EvaluateCartLevelPromotionsAsync(cartTargets, result.Subtotal, cancellationToken);
                if (cartLevelEval.HasCartLevelPromotion)
                {
                    result.CartLevelDiscountAmount = cartLevelEval.TotalCartDiscount;
                    result.CartLevelPromotionName = cartLevelEval.PromotionName;
                }
            }

            result.Total = Math.Max(0, result.Subtotal - result.CartLevelDiscountAmount - result.DiscountAmount);
            result.TotalAmount = result.Total;

            return result;
        }
    }
}
