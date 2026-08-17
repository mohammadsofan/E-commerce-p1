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

        protected CartDto Map(Cart cart) => Mapper.Map<CartDto>(cart);
    }
}
