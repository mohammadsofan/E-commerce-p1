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

            var productsTask = Db.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, product.Slug, product.CategoryId, product.BasePrice })
                .ToListAsync(cancellationToken);

            var imagesTask = Db.ProductImages
                .AsNoTracking()
                .Where(image => productIds.Contains(image.ProductId))
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(productsTask, imagesTask);
            var products = await productsTask;
            var images = await imagesTask;

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
                item.ProductSlug = prod?.Slug ?? string.Empty;
                item.ImageUrl = images
                    .Where(image => image.ProductId == item.ProductId && image.ProductVariantId == item.ProductVariantId)
                    .Select(image => image.Url)
                    .FirstOrDefault()
                    ?? images
                        .Where(image => image.ProductId == item.ProductId && image.ProductVariantId == null)
                        .Select(image => image.Url)
                        .FirstOrDefault();

                if (promoEvaluations != null && promoEvaluations.TryGetValue(item.ProductId, out var eval) && eval.HasActivePromotion)
                {
                    item.OriginalPrice = prod?.BasePrice ?? item.UnitPrice;
                    item.PromotionalPrice = eval.PromotionalPrice;
                    item.PromotionName = eval.PromotionName;
                    item.PromotionBadge = eval.PromotionBadge;
                }
            }

            return result;
        }
    }
}
