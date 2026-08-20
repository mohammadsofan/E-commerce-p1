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

        protected CartAccessor(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
        {
            Db = db;
            CurrentUser = currentUser;
            Mapper = mapper;
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

            var products = await Db.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, product.Slug })
                .ToListAsync(cancellationToken);

            var images = await Db.ProductImages
                .AsNoTracking()
                .Where(image => productIds.Contains(image.ProductId))
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder)
                .ToListAsync(cancellationToken);

            foreach (var item in result.Items)
            {
                item.ProductSlug = products
                    .Where(product => product.Id == item.ProductId)
                    .Select(product => product.Slug)
                    .FirstOrDefault() ?? string.Empty;
                item.ImageUrl = images
                    .Where(image => image.ProductId == item.ProductId && image.ProductVariantId == item.ProductVariantId)
                    .Select(image => image.Url)
                    .FirstOrDefault()
                    ?? images
                        .Where(image => image.ProductId == item.ProductId && image.ProductVariantId == null)
                        .Select(image => image.Url)
                        .FirstOrDefault();
            }

            return result;
        }
    }
}
