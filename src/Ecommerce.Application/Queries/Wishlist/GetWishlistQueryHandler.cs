using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Wishlist
{
    public class GetWishlistQueryHandler : IQueryHandler<GetWishlistQuery, List<WishlistItemDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetWishlistQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<List<WishlistItemDto>> Handle(GetWishlistQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                return new List<WishlistItemDto>();
            }

            var items = await _db.WishlistItems
                .AsNoTracking()
                .Where(w => w.UserId == userId.Value)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Images)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Brand)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.InventoryItems)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken);

            return items
                .Where(w => w.Product != null && !w.Product.IsDeleted)
                .Select(w =>
                {
                    var product = w.Product!;
                    var primaryImage = product.Images?.FirstOrDefault(i => i.IsPrimary)?.Url 
                                       ?? product.Images?.FirstOrDefault()?.Url;
                    var availableStock = product.InventoryItems?
                        .Sum(i => Math.Max(0, i.QuantityOnHand - i.QuantityReserved)) ?? 0;

                    return new WishlistItemDto
                    {
                        Id = w.Id,
                        ProductId = w.ProductId,
                        ProductName = product.Name,
                        ProductSlug = product.Slug,
                        ProductPrice = product.BasePrice,
                        ProductImageUrl = primaryImage,
                        AvailableStock = availableStock,
                        IsActive = product.IsActive,
                        CategoryName = product.Category?.Name,
                        BrandName = product.Brand?.Name,
                        CreatedAt = w.CreatedAt
                    };
                })
                .ToList();
        }
    }
}
