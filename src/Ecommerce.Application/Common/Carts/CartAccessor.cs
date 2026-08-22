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
                var targets = products.Select(p => new ProductPromotionTarget
                {
                    ProductId = p.Id,
                    CategoryId = p.CategoryId,
                    BasePrice = p.BasePrice
                });
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

                item.OriginalPrice = prod?.BasePrice ?? item.UnitPrice;

                if (item.UnitPrice == 0 && prod != null && prod.BasePrice > 0)
                {
                    item.UnitPrice = prod.BasePrice;
                    item.LineTotal = item.UnitPrice * item.Quantity;
                }

                if (promoEvaluations != null && promoEvaluations.TryGetValue(item.ProductId, out var eval) && eval.HasActivePromotion)
                {
                    if (eval.DiscountAmount > 0 && eval.PromotionalPrice < item.OriginalPrice)
                    {
                        item.PromotionalPrice = eval.PromotionalPrice;
                    }
                    item.PromotionName = eval.PromotionName;
                    item.PromotionBadge = eval.PromotionBadge;
                }
            }

            result.Subtotal = result.Items.Sum(i => i.LineTotal);
            result.Total = Math.Max(0, result.Subtotal - result.DiscountAmount);
            result.TotalAmount = result.Total;

            return result;
        }
    }
}
