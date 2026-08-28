using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Inventory;
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

            // A product that is not on sale must not be purchasable, even via a direct API call.
            if (!product.IsActive || product.IsDeleted)
                throw new DomainException("هذا المنتج غير متاح للبيع حالياً.");

            decimal unitPrice = product.BasePrice;
            string productName = product.Name;
            bool allowBackorder = product.AllowBackorder;

            if (command.ProductVariantId is Guid variantId && variantId != Guid.Empty)
            {
                var variant = await Db.ProductVariants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
                if (variant == null) throw new NotFoundException("ProductVariant", variantId);

                // The variant must belong to the product being added, otherwise a caller
                // could borrow any variant's price for any product.
                if (variant.ProductId != command.ProductId)
                    throw new DomainException("الخيار المحدد لا ينتمي إلى هذا المنتج.");

                if (!variant.IsActive)
                    throw new DomainException("الخيار المحدد غير متاح حالياً.");

                unitPrice = variant.Price;
                productName = string.IsNullOrWhiteSpace(variant.Name) ? product.Name : variant.Name;
                allowBackorder = variant.AllowBackorder;
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

                var isMerge = existing != null && await Db.CartItems.AnyAsync(i => i.Id == existing.Id, cancellationToken);
                var requestedTotal = isMerge ? existing!.Quantity + command.Quantity : command.Quantity;

                // --- JIT Inventory Validation ---
                // Validated against the same allocator the checkout uses, so a line that is
                // accepted here cannot be rejected at checkout for stock reasons.
                await EnsureStockAvailableAsync(command, allowBackorder, product.TrackInventory, requestedTotal, isMerge, cancellationToken);
                // --------------------------------

                if (isMerge)
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

        private async Task EnsureStockAvailableAsync(
            AddToCartCommand command,
            bool allowBackorder,
            bool trackInventory,
            int requestedTotal,
            bool isMerge,
            CancellationToken cancellationToken)
        {
            if (allowBackorder) return;

            var variantId = command.ProductVariantId.HasValue && command.ProductVariantId.Value != Guid.Empty
                ? command.ProductVariantId
                : null;

            var inventoryRows = await Db.InventoryItems
                .AsNoTracking()
                .Where(inv => inv.ProductId == command.ProductId ||
                              (inv.ProductVariantId.HasValue && variantId.HasValue && inv.ProductVariantId == variantId.Value))
                .ToListAsync(cancellationToken);

            var candidates = InventoryAllocator.CandidatesFor(inventoryRows, command.ProductId, variantId);

            if (candidates.Count == 0 && !trackInventory) return;

            // A row flagged for backorder makes the line unbounded regardless of the
            // product/variant flag, matching the checkout allocator.
            if (InventoryAllocator.AllowsBackorder(candidates)) return;

            var totalAvailable = InventoryAllocator.AvailableFor(candidates, requestedTotal);

            if (requestedTotal > totalAvailable)
            {
                if (totalAvailable <= 0)
                    throw new DomainException("المنتج غير متوفر حالياً في المخزون.");

                throw new DomainException(isMerge
                    ? $"الكمية الإجمالية المطلوبة ({requestedTotal}) تتجاوز المخزون المتاح ({totalAvailable})."
                    : $"الكمية المطلوبة ({requestedTotal}) تتجاوز المخزون المتاح ({totalAvailable}).");
            }
        }
    }
}
