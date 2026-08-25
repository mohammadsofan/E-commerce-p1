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

            var staleOrders = await db.Orders
                .Include(o => o.Items)
                .Where(o => o.Status == OrderStatus.Placed && o.CreatedAt <= cutoff)
                .ToListAsync(cancellationToken);

            if (!staleOrders.Any())
            {
                return 0;
            }

            foreach (var order in staleOrders)
            {
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
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cancelled {Count} abandoned orders and released inventory.", staleOrders.Count);
            return staleOrders.Count;
        }
    }
}
