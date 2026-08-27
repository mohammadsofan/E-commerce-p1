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
                var cartItem = cart.Items.FirstOrDefault(i => i.Id == command.CartItemId);
                if (cartItem != null)
                {
                    var prod = await Db.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId, cancellationToken);
                    bool trackInventory = prod?.TrackInventory ?? false;
                    bool allowBackorder = false;

                    if (cartItem.ProductVariantId.HasValue && cartItem.ProductVariantId.Value != System.Guid.Empty)
                    {
                        var variant = await Db.ProductVariants
                            .AsNoTracking()
                            .FirstOrDefaultAsync(v => v.Id == cartItem.ProductVariantId.Value, cancellationToken);
                        allowBackorder = variant?.AllowBackorder ?? false;
                    }
                    else
                    {
                        allowBackorder = prod?.AllowBackorder ?? false;
                    }

                    if (!allowBackorder)
                    {
                        var inventoryQuery = cartItem.ProductVariantId.HasValue && cartItem.ProductVariantId.Value != System.Guid.Empty
                            ? Db.InventoryItems.Where(inv => inv.ProductVariantId == cartItem.ProductVariantId.Value && !inv.AllowBackorder)
                            : Db.InventoryItems.Where(inv => inv.ProductId == cartItem.ProductId && !inv.ProductVariantId.HasValue && !inv.AllowBackorder);

                        var hasInventoryRecords = await inventoryQuery.AnyAsync(cancellationToken);
                        if (hasInventoryRecords || trackInventory)
                        {
                            var totalAvailable = await inventoryQuery
                                .AsNoTracking()
                                .SumAsync(inv => inv.QuantityOnHand - inv.QuantityReserved, cancellationToken);

                            if (command.Quantity > totalAvailable)
                            {
                                throw new DomainException(totalAvailable <= 0
                                    ? "المنتج غير متوفر حالياً في المخزون."
                                    : $"الكمية المطلوبة ({command.Quantity}) تتجاوز المخزون المتاح ({totalAvailable}).");
                            }
                        }
                    }
                }
            }

            cart.UpdateItemQuantity(command.CartItemId, command.Quantity);

            await Db.SaveChangesAsync(cancellationToken);
            return await MapAsync(cart, cancellationToken);
        }
    }
}
