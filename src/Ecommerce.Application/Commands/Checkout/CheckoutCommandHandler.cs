using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Application.Commands.Checkout
{
    public class CheckoutCommandHandler : Ecommerce.Application.Common.Commands.ICommandHandler<CheckoutCommand, System.Guid>
    {
        private readonly IApplicationDbContext _db;

        public CheckoutCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<System.Guid> Handle(CheckoutCommand command, CancellationToken cancellationToken = default)
        {
            if (command.Items == null || !command.Items.Any()) throw new DomainException("No items to checkout");

            // Build order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0,6)}",
                CurrencyCode = command.Currency,
                ShippingAmount = 0m
            };

            foreach (var it in command.Items)
            {
                // In a full implementation, lookup product details
                order.AddItem(it.ProductId, it.ProductVariantId, "Product", 10m, it.Quantity);

                // Reserve inventory
                var inventory = await _db.InventoryItems.FindAsync(new object[] { it.ProductVariantId }, cancellationToken);
                if (inventory == null)
                {
                    // Try by product id
                    inventory = await _db.InventoryItems.FindAsync(new object[] { it.ProductId }, cancellationToken);
                }

                if (inventory == null)
                {
                    throw new InventoryException("Inventory item not found for product/variant");
                }

                inventory.Reserve(it.Quantity);
            }

            order.PlaceOrder();

            // Persist order
            // Note: ApplicationDbContext should expose Orders DbSet
            var db = (dynamic)_db;
            await db.Orders.AddAsync(order);
            await _db.SaveChangesAsync(cancellationToken);

            return order.Id;
        }
    }
}
