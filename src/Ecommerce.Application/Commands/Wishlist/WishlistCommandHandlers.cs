using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Wishlist
{
    public class AddToWishlistCommandHandler : ICommandHandler<AddToWishlistCommand, WishlistItemDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public AddToWishlistCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<WishlistItemDto> Handle(AddToWishlistCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                throw new DomainException("User must be authenticated to add items to wishlist.");
            }

            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.InventoryItems)
                .FirstOrDefaultAsync(p => p.Id == command.ProductId && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                throw new NotFoundException(nameof(Product), command.ProductId);
            }

            var existing = await _db.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId.Value && w.ProductId == command.ProductId, cancellationToken);

            WishlistItem item;
            if (existing != null)
            {
                item = existing;
            }
            else
            {
                item = WishlistItem.Create(userId.Value, command.ProductId);
                _db.WishlistItems.Add(item);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var primaryImage = product.Images?.FirstOrDefault(i => i.IsPrimary)?.Url 
                               ?? product.Images?.FirstOrDefault()?.Url;
            var availableStock = product.InventoryItems?
                .Sum(i => Math.Max(0, i.QuantityOnHand - i.QuantityReserved)) ?? 0;

            return new WishlistItemDto
            {
                Id = item.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                ProductPrice = product.BasePrice,
                ProductImageUrl = primaryImage,
                AvailableStock = availableStock,
                IsActive = product.IsActive,
                CategoryName = product.Category?.Name,
                BrandName = product.Brand?.Name,
                CreatedAt = item.CreatedAt
            };
        }
    }

    public class RemoveFromWishlistCommandHandler : ICommandHandler<RemoveFromWishlistCommand, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public RemoveFromWishlistCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(RemoveFromWishlistCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                return Unit.Value;
            }

            var item = await _db.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId.Value && (w.ProductId == command.ProductId || w.Id == command.ProductId), cancellationToken);

            if (item != null)
            {
                _db.WishlistItems.Remove(item);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }

    public class ClearWishlistCommandHandler : ICommandHandler<ClearWishlistCommand, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ClearWishlistCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(ClearWishlistCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                return Unit.Value;
            }

            var items = await _db.WishlistItems
                .Where(w => w.UserId == userId.Value)
                .ToListAsync(cancellationToken);

            if (items.Any())
            {
                _db.WishlistItems.RemoveRange(items);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }
}
