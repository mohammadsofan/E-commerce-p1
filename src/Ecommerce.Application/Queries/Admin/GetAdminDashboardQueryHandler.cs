using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using DomainEntities = Ecommerce.Domain.Entities;
using System.Linq.Expressions;
using Order = Ecommerce.Domain.Entities.Order;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminDashboardQueryHandler : IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>
    {
        private readonly IApplicationDbContext _db;

        public GetAdminDashboardQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery query, CancellationToken cancellationToken = default)
        {
            // Basic counts
            var totalProducts = await _db.Products.CountAsync(cancellationToken);
            var totalOrders = await _db.Orders.CountAsync(cancellationToken);
            var totalCustomers = await _db.Users.CountAsync(cancellationToken);

            // Revenue calculations
            var completedOrders = await _db.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .Select(o => new { o.TotalAmount, o.CreatedAt })
                .ToListAsync(cancellationToken);

            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);

            var pendingOrders = await _db.Orders
                .Where(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.Paid)
                .Select(o => o.TotalAmount)
                .ToListAsync(cancellationToken);

            var pendingOrdersRevenue = pendingOrders.Sum();

            // Low stock and out of stock products
            var productsWithInventory = await _db.InventoryItems
                .Where(i => i.ProductId != null)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Available = g.Sum(i => i.QuantityOnHand - i.QuantityReserved)
                })
                .ToListAsync(cancellationToken);

            var lowStockProducts = productsWithInventory.Count(p => p.Available > 0 && p.Available <= 10);
            var outOfStockProducts = productsWithInventory.Count(p => p.Available <= 0);

            // Sales by period (last 7 days)
            var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
            var recentOrders = await _db.Orders
                .Where(o => o.CreatedAt >= sevenDaysAgo)
                .ToListAsync(cancellationToken);

            var salesByPeriod = recentOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new SalesByPeriodDto
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                });

            var salesByPeriodList = (from s in salesByPeriod
                                     orderby s.Period
                                     select s).ToList();

            // Top products - join with Orders to filter by status
            var topProductsQuery = from oi in _db.OrderItems
                                   join o in _db.Orders on oi.OrderId equals o.Id
                                   where o.Status == OrderStatus.Completed
                                   group oi by oi.ProductId into g
                                   select new TopProductDto
                                   {
                                       ProductId = g.Key,
                                       ProductName = g.First().ProductName,
                                       TotalSold = g.Sum(x => x.Quantity),
                                       Revenue = g.Sum(x => x.TotalAmount)
                                   };

            var topProducts = await topProductsQuery
                .OrderByDescending(p => p.TotalSold)
                .Take(10)
                .ToListAsync(cancellationToken);

            // Top customers
            var customerOrders = await _db.Orders
                .Where(o => o.UserId != null)
                .GroupBy(o => o.UserId!.Value)
                .Select(g => new TopCustomerDto
                {
                    CustomerId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToListAsync(cancellationToken);

            // Get customer names
            var customerIds = customerOrders.Select(c => c.CustomerId).ToList();
            var customers = await _db.Users
                .Where(u => customerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName, cancellationToken);

            foreach (var customer in customerOrders)
            {
                if (customers.TryGetValue(customer.CustomerId, out var name))
                {
                    customer.CustomerName = name;
                }
            }

            // Build final dashboard
            var dashboard = new AdminDashboardDto
            {
                TotalProducts = await _db.Products.CountAsync(cancellationToken),
                TotalOrders = await _db.Orders.CountAsync(cancellationToken),
                TotalCustomers = await _db.Users.CountAsync(cancellationToken),
                TotalRevenue = totalRevenue,
                PendingOrdersRevenue = pendingOrdersRevenue,
                LowStockProducts = lowStockProducts,
                OutOfStockProducts = outOfStockProducts,
                SalesByPeriod = salesByPeriodList,
                TopProducts = topProducts,
                TopCustomers = customerOrders
            };

            return dashboard;
        }
    }
}