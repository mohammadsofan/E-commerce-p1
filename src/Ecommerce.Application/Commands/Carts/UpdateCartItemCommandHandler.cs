using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Carts
{
    public class UpdateCartItemCommandHandler : CartAccessor, ICommandHandler<UpdateCartItemCommand, CartDto>
    {
        public UpdateCartItemCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
        {
        }

        public async Task<CartDto> Handle(UpdateCartItemCommand command, CancellationToken cancellationToken = default)
        {
            // A quantity <= 0 removes the line (handled inside the aggregate).
            var cart = await GetCartOrThrowAsync(cancellationToken);

            if (command.Quantity > 0)
            {
                // Locate the cart item to determine which product/variant is being updated.
                var cartItem = cart.Items.FirstOrDefault(i => i.Id == command.CartItemId);
                if (cartItem != null)
                {
                    // JIT stock validation: ensure the new quantity doesn't exceed total available
                    // stock across all warehouses for this specific product/variant combination.
                    var inventoryQuery = cartItem.ProductVariantId.HasValue
                        ? Db.InventoryItems.Where(inv => inv.ProductVariantId == cartItem.ProductVariantId.Value && !inv.AllowBackorder)
                        : Db.InventoryItems.Where(inv => inv.ProductId == cartItem.ProductId && !inv.ProductVariantId.HasValue && !inv.AllowBackorder);

                    var totalAvailable = await inventoryQuery
                        .AsNoTracking()
                        .SumAsync(inv => inv.QuantityOnHand - inv.QuantityReserved, cancellationToken);

                    if (totalAvailable > 0 && command.Quantity > totalAvailable)
                    {
                        throw new DomainException($"الكمية المطلوبة ({command.Quantity}) تتجاوز المخزون المتاح ({totalAvailable}).");
                    }
                }
            }

            cart.UpdateItemQuantity(command.CartItemId, command.Quantity);

            await Db.SaveChangesAsync(cancellationToken);
            return await MapAsync(cart, cancellationToken);
        }
    }
}
