using System;
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
    public class AddToCartCommandHandler : CartAccessor, ICommandHandler<AddToCartCommand, CartDto>
    {
        public AddToCartCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
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

            var cart = await GetOrCreateCartAsync(cancellationToken);
            cart.AddItem(product.Id, command.ProductVariantId, productName, unitPrice, command.Quantity);

            await Db.SaveChangesAsync(cancellationToken);
            return Map(cart);
        }
    }
}
