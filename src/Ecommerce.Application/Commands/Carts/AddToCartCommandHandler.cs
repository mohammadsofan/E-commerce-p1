using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Carts
{
    public class AddToCartCommandHandler : CartAccessor, ICommandHandler<AddToCartCommand, CartDto>
    {
        // Cart writes must not interleave while the aggregate is being loaded and saved.
        // This prevents two requests from trying to persist the same cart snapshot.
        private static readonly SemaphoreSlim CartWriteLock = new(1, 1);

        public AddToCartCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IMapper mapper,
            IPromotionEvaluationService? promotionEvaluator = null)
            : base(db, currentUser, mapper, promotionEvaluator)
        {
        }

        public async Task<CartDto> Handle(AddToCartCommand command, CancellationToken cancellationToken = default)
        {
            // Server-authoritative: the client only supplies the product/variant and quantity;
            // name and price come from the catalog so they cannot be tampered with.
            // AsNoTracking: the handler only reads catalog data (name/price) and never
            // modifies the Product, so tracking it would needlessly pollute the change tracker
            // and can interfere with concurrency tokens (e.g. Product.RowVersion) on SaveChanges.
            var product = await Db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
            if (product == null) throw new NotFoundException("Product", command.ProductId);

            decimal unitPrice = product.BasePrice;
            string productName = product.Name;

            if (command.ProductVariantId is Guid variantId)
            {
                var variant = await Db.ProductVariants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
                if (variant == null) throw new NotFoundException("ProductVariant", variantId);

                unitPrice = variant.Price;
                productName = string.IsNullOrWhiteSpace(variant.Name) ? product.Name : variant.Name;
            }

            // Evaluate automatic promotional discount
            if (PromotionEvaluator != null)
            {
                var promoEval = await PromotionEvaluator.EvaluateProductAsync(
                    product.Id,
                    product.CategoryId,
                    unitPrice,
                    cancellationToken);

                if (promoEval.HasActivePromotion && promoEval.DiscountAmount > 0 && promoEval.PromotionalPrice < unitPrice)
                {
                    unitPrice = promoEval.PromotionalPrice;
                }
            }

            var normalizedOptions = string.IsNullOrWhiteSpace(command.SelectedOptions) ? null : command.SelectedOptions.Trim();

            await CartWriteLock.WaitAsync(cancellationToken);
            try
            {
                var cart = await GetOrCreateCartAsync(cancellationToken);
                var existing = cart.Items.FirstOrDefault(i =>
                    i.ProductId == product.Id &&
                    i.ProductVariantId == command.ProductVariantId &&
                    (string.IsNullOrWhiteSpace(i.SelectedOptions) ? null : i.SelectedOptions.Trim()) == normalizedOptions);
                if (existing != null && await Db.CartItems.AnyAsync(i => i.Id == existing.Id, cancellationToken))
                {
                    cart.AddItem(product.Id, command.ProductVariantId, productName, unitPrice, command.Quantity, normalizedOptions);
                }
                else
                {
                    // Do not let a stale tracked item make EF issue an UPDATE for a
                    // row that no longer exists in the database.
                    var item = CartItem.Create(
                        cart.Id,
                        product.Id,
                        command.ProductVariantId,
                        productName,
                        unitPrice,
                        command.Quantity,
                        normalizedOptions);
                    cart.Items.Add(item);
                    Db.CartItems.Add(item);
                }
                await Db.SaveChangesAsync(cancellationToken);
                return await MapAsync(cart, cancellationToken);
            }
            finally
            {
                CartWriteLock.Release();
            }
        }
    }
}
