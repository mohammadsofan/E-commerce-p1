using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    public class AbandonedOrderCleanupService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<AbandonedOrderCleanupService> _logger;
        private readonly TimeSpan _checkInterval;
        private readonly TimeSpan _orderTimeout;

        public AbandonedOrderCleanupService(
            IServiceProvider provider,
            ILogger<AbandonedOrderCleanupService> logger)
            : this(provider, logger, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
        {
        }

        public AbandonedOrderCleanupService(
            IServiceProvider provider,
            ILogger<AbandonedOrderCleanupService> logger,
            TimeSpan checkInterval,
            TimeSpan orderTimeout)
        {
            _provider = provider;
            _logger = logger;
            _checkInterval = checkInterval;
            _orderTimeout = orderTimeout;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAbandonedOrdersAsync(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during abandoned order cleanup.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        public async Task<int> CleanupAbandonedOrdersAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var cutoff = DateTimeOffset.UtcNow.Subtract(_orderTimeout);

            // Order.Cancel() refuses to cancel once fulfilment has started, so an order that was
            // shipped or delivered while still sitting in Placed can never be closed by this
            // worker. Filtering those rows out in the query keeps the batch free of orders that
            // are guaranteed to throw.
            //
            // Only the ids are selected up front: each order is then loaded, cancelled and saved
            // in its own iteration, so a failure can be isolated without discarding work that
            // has already been done or entities that are still pending.
            var staleOrderIds = await db.Orders
                .Where(o => o.Status == OrderStatus.Placed
                            && o.FulfillmentStatus != FulfillmentStatus.Shipped
                            && o.FulfillmentStatus != FulfillmentStatus.Delivered
                            && o.CreatedAt <= cutoff)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            if (staleOrderIds.Count == 0)
            {
                return 0;
            }

            var cancelledCount = 0;

            foreach (var orderId in staleOrderIds)
            {
                // Each order is cancelled and saved on its own. A single unclosable order used to
                // abort the whole batch: the loop had no per-order guard and one shared
                // SaveChangesAsync at the end, so every other stale order stayed Placed and its
                // inventory stayed reserved forever.
                try
                {
                    var order = await db.Orders
                        .Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                    // Cancelled, shipped or delivered in the window between the id scan and now.
                    if (order == null || order.Status != OrderStatus.Placed) continue;

                    order.Cancel("Abandoned order timeout");

                    if (order.Items != null && order.Items.Any())
                    {
                        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
                        var variantIds = order.Items
                            .Where(i => i.ProductVariantId != Guid.Empty)
                            .Select(i => i.ProductVariantId)
                            .Distinct()
                            .ToList();

                        var inventoryItems = await db.InventoryItems
                            .Where(inv => productIds.Contains(inv.ProductId) ||
                                          (inv.ProductVariantId.HasValue && variantIds.Contains(inv.ProductVariantId.Value)))
                            .ToListAsync(cancellationToken);

                        foreach (var item in order.Items)
                        {
                            var matchingInventory = inventoryItems
                                .Where(inv =>
                                    (item.ProductVariantId != Guid.Empty && inv.ProductVariantId == item.ProductVariantId)
                                    || (item.ProductVariantId == Guid.Empty && inv.ProductId == item.ProductId && !inv.ProductVariantId.HasValue))
                                .OrderByDescending(inv => inv.QuantityReserved)
                                .ToList();

                            int remainingToRelease = item.Quantity;
                            foreach (var inv in matchingInventory)
                            {
                                if (remainingToRelease <= 0) break;
                                if (inv.QuantityReserved <= 0) continue;

                                int canRelease = Math.Min(remainingToRelease, inv.QuantityReserved);
                                if (canRelease > 0)
                                {
                                    inv.Release(canRelease);
                                    remainingToRelease -= canRelease;
                                }
                            }
                        }
                    }

                    await db.SaveChangesAsync(cancellationToken);
                    cancelledCount++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Drop the failed order's pending changes so they cannot leak into the next
                    // iteration's SaveChangesAsync, then carry on with the rest of the batch.
                    db.ClearChangeTracker();
                    _logger.LogWarning(
                        ex,
                        "Skipped abandoned order {OrderId} during cleanup; continuing with the remaining orders.",
                        orderId);
                }
            }

            _logger.LogInformation(
                "Cancelled {Count} of {Total} abandoned orders and released inventory.",
                cancelledCount,
                staleOrderIds.Count);

            return cancelledCount;
        }
    }
}
