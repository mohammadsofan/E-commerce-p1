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
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetSalesReportQueryHandler : IQueryHandler<GetSalesReportQuery, SalesReportDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetSalesReportQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<SalesReportDto> Handle(GetSalesReportQuery query, CancellationToken cancellationToken = default)
        {
            var endDate = query.EndDate ?? DateTimeOffset.UtcNow;
            var startDate = query.StartDate ?? endDate.AddDays(-30);

            var orders = await _db.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status != OrderStatus.Cancelled)
                .ToListAsync(cancellationToken);

            var orderIds = orders.Select(o => o.Id).ToList();
            var orderItems = await _db.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync(cancellationToken);

            var customerIds = orders.Where(o => o.UserId.HasValue).Select(o => o.UserId!.Value).Distinct().ToList();
            var newCustomerIds = await _db.Orders
                .Where(o => o.UserId.HasValue && customerIds.Contains(o.UserId.Value) && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .GroupBy(o => o.UserId!.Value)
                .Where(g => g.Min(o => o.CreatedAt) >= startDate)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            var salesByPeriod = orders
                .GroupBy(o => GetPeriodKey(o.CreatedAt, query.GroupBy))
                .Select(g => new SalesByPeriodDto
                {
                    Period = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    NewCustomers = g.Count(o => o.UserId.HasValue && newCustomerIds.Contains(o.UserId.Value))
                })
                .OrderBy(x => x.Period)
                .ToList();

            var topProducts = orderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key,
                    ProductName = g.First().ProductName,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalAmount)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(10)
                .ToList();

            var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToList();
            var productsWithCategory = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.CategoryId, cancellationToken);

            var topCategories = orderItems
                .Where(oi => productsWithCategory.ContainsKey(oi.ProductId) && productsWithCategory[oi.ProductId] != null)
                .GroupBy(oi => productsWithCategory[oi.ProductId]!.Value)
                .Select(g => new TopCategoryDto
                {
                    CategoryId = g.Key,
                    CategoryName = g.First().ProductName,
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    Revenue = g.Sum(oi => oi.TotalAmount)
                })
                .OrderByDescending(c => c.Revenue)
                .Take(10)
                .ToList();

            var categoryIds = topCategories.Select(c => c.CategoryId).ToList();
            var categories = await _db.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            foreach (var cat in topCategories)
            {
                if (categories.TryGetValue(cat.CategoryId, out var name))
                    cat.CategoryName = name;
            }

            return new SalesReportDto
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                NewCustomers = newCustomerIds.Count,
                ReturningCustomers = customerIds.Count - newCustomerIds.Count,
                ConversionRate = 0,
                SalesByPeriod = salesByPeriod,
                TopProducts = topProducts,
                TopCategories = topCategories
            };
        }

        private string GetPeriodKey(DateTimeOffset date, string groupBy)
        {
            return groupBy switch
            {
                "week" => $"{date.Year}-W{GetWeekOfYear(date):D2}",
                "month" => date.ToString("yyyy-MM"),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        private int GetWeekOfYear(DateTimeOffset date)
        {
            var jan1 = new DateTimeOffset(date.Year, 1, 1, 0, 0, 0, date.Offset);
            var days = (date - jan1).TotalDays;
            return (int)Math.Ceiling((days + (int)jan1.DayOfWeek) / 7.0);
        }
    }

    public class GetRevenueReportQueryHandler : IQueryHandler<GetRevenueReportQuery, RevenueReportDto>
    {
        private readonly IApplicationDbContext _db;

        public GetRevenueReportQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<RevenueReportDto> Handle(GetRevenueReportQuery query, CancellationToken cancellationToken = default)
        {
            var endDate = query.EndDate ?? DateTimeOffset.UtcNow;
            var startDate = query.StartDate ?? endDate.AddDays(-30);

            var orders = await _db.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status != OrderStatus.Cancelled)
                .ToListAsync(cancellationToken);

            var refunds = await _db.Refunds
                .Where(r => r.ProcessedAt >= startDate && r.ProcessedAt <= endDate && r.Status == "succeeded")
                .ToListAsync(cancellationToken);

            var totalDiscounts = orders.Sum(o => o.DiscountAmount);
            var totalTax = orders.Sum(o => o.TaxAmount);
            var totalShipping = orders.Sum(o => o.ShippingAmount);

            var revenueByPeriod = orders
                .GroupBy(o => GetPeriodKey(o.CreatedAt, query.GroupBy))
                .Select(g => new RevenueByPeriodDto
                {
                    Period = g.Key,
                    GrossRevenue = g.Sum(o => o.TotalAmount),
                    NetRevenue = g.Sum(o => o.TotalAmount),
                    Discounts = g.Sum(o => o.DiscountAmount),
                    Refunds = 0
                })
                .OrderBy(x => x.Period)
                .ToList();

            var refundsByPeriod = refunds
                .GroupBy(r => GetPeriodKey(r.ProcessedAt!.Value, query.GroupBy))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

            foreach (var period in revenueByPeriod)
            {
                if (refundsByPeriod.TryGetValue(period.Period, out var refundAmount))
                {
                    period.Refunds = refundAmount;
                    period.NetRevenue -= refundAmount;
                }
            }

            var revenueByChannel = new List<RevenueByChannelDto>
            {
                new RevenueByChannelDto { Channel = "Online", Revenue = orders.Sum(o => o.TotalAmount), OrderCount = orders.Count }
            };

            var grossRevenue = orders.Sum(o => o.TotalAmount);
            var totalRefunds = refunds.Sum(r => r.Amount);

            return new RevenueReportDto
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                GrossRevenue = grossRevenue,
                NetRevenue = grossRevenue - totalRefunds,
                TotalDiscounts = totalDiscounts,
                TotalRefunds = totalRefunds,
                TotalTax = totalTax,
                TotalShipping = totalShipping,
                RevenueByPeriod = revenueByPeriod,
                RevenueByChannel = revenueByChannel
            };
        }

        private string GetPeriodKey(DateTimeOffset date, string groupBy)
        {
            return groupBy switch
            {
                "week" => $"{date.Year}-W{GetWeekOfYear(date):D2}",
                "month" => date.ToString("yyyy-MM"),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        private int GetWeekOfYear(DateTimeOffset date)
        {
            var jan1 = new DateTimeOffset(date.Year, 1, 1, 0, 0, 0, date.Offset);
            var days = (date - jan1).TotalDays;
            return (int)Math.Ceiling((days + (int)jan1.DayOfWeek) / 7.0);
        }
    }

    public class GetInventoryReportQueryHandler : IQueryHandler<GetInventoryReportQuery, InventoryReportDto>
    {
        private readonly IApplicationDbContext _db;

        public GetInventoryReportQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<InventoryReportDto> Handle(GetInventoryReportQuery query, CancellationToken cancellationToken = default)
        {
            var asOfDate = query.AsOfDate ?? DateTimeOffset.UtcNow;

            var inventoryQuery = _db.InventoryItems.AsQueryable();
            if (query.WarehouseIds.Any())
                inventoryQuery = inventoryQuery.Where(i => query.WarehouseIds.Contains(i.WarehouseId));

            var inventoryItems = await inventoryQuery
                .Include(i => i.Product)
                .Include(i => i.ProductVariant)
                .Include(i => i.Warehouse)
                .ToListAsync(cancellationToken);

            if (query.CategoryIds.Any())
            {
                var productIdsInCategories = await _db.Products
                    .Where(p => p.CategoryId != null && query.CategoryIds.Contains(p.CategoryId.Value))
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                inventoryItems = inventoryItems
                    .Where(i => (i.ProductId != Guid.Empty && productIdsInCategories.Contains(i.ProductId)) ||
                                (i.ProductVariantId != Guid.Empty && i.ProductVariant != null && productIdsInCategories.Contains(i.ProductVariant.ProductId)))
                    .ToList();
            }

            var byWarehouse = inventoryItems
                .GroupBy(i => new { i.WarehouseId, i.Warehouse?.Name })
                .Select(g => new InventoryByWarehouseDto
                {
                    WarehouseId = g.Key.WarehouseId,
                    WarehouseName = g.Key.Name ?? string.Empty,
                    ProductCount = g.Where(i => i.ProductId != Guid.Empty).Select(i => i.ProductId).Distinct().Count(),
                    VariantCount = g.Where(i => i.ProductVariantId != Guid.Empty).Select(i => i.ProductVariantId).Distinct().Count(),
                    TotalValue = g.Sum(i => i.Available * (i.ProductVariant?.Price ?? i.Product?.BasePrice ?? 0)),
                    LowStockCount = g.Count(i => i.Available > 0 && i.Available <= 10)
                })
                .ToList();

            var byCategory = inventoryItems
                .Where(i => i.Product != null && i.Product.CategoryId != null)
                .GroupBy(i => i.Product!.CategoryId!.Value)
                .Select(g => new InventoryByCategoryDto
                {
                    CategoryId = g.Key,
                    CategoryName = string.Empty,
                    ProductCount = g.Where(i => i.ProductId != Guid.Empty).Select(i => i.ProductId).Distinct().Count(),
                    VariantCount = g.Where(i => i.ProductVariantId != Guid.Empty).Select(i => i.ProductVariantId).Distinct().Count(),
                    TotalValue = g.Sum(i => i.Available * (i.ProductVariant?.Price ?? i.Product?.BasePrice ?? 0))
                })
                .ToList();

            // Get category names
            var categoryIds = byCategory.Select(c => c.CategoryId).ToList();
            var categories = await _db.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            foreach (var cat in byCategory)
            {
                if (categories.TryGetValue(cat.CategoryId, out var name))
                    cat.CategoryName = name;
            }

            return new InventoryReportDto
            {
                AsOfDate = asOfDate,
                TotalProducts = inventoryItems.Where(i => i.ProductId != Guid.Empty).Select(i => i.ProductId).Distinct().Count(),
                TotalVariants = inventoryItems.Where(i => i.ProductVariantId != Guid.Empty).Select(i => i.ProductVariantId).Distinct().Count(),
                LowStockCount = inventoryItems.Count(i => i.Available > 0 && i.Available <= 10),
                OutOfStockCount = inventoryItems.Count(i => i.Available <= 0),
                TotalInventoryValue = inventoryItems.Sum(i => i.Available * (i.ProductVariant?.Price ?? i.Product?.BasePrice ?? 0)),
                ByWarehouse = byWarehouse,
                ByCategory = byCategory
            };
        }
    }

    public class GetCustomerReportQueryHandler : IQueryHandler<GetCustomerReportQuery, CustomerReportDto>
    {
        private readonly IApplicationDbContext _db;

        public GetCustomerReportQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CustomerReportDto> Handle(GetCustomerReportQuery query, CancellationToken cancellationToken = default)
        {
            var endDate = query.EndDate ?? DateTimeOffset.UtcNow;
            var startDate = query.StartDate ?? endDate.AddDays(-30);

            var customers = await _db.Users.ToListAsync(cancellationToken);
            var orders = await _db.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status != OrderStatus.Cancelled)
                .ToListAsync(cancellationToken);

            var customerOrders = orders
                .Where(o => o.UserId.HasValue)
                .GroupBy(o => o.UserId.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    FirstOrder = g.Min(o => o.CreatedAt),
                    LastOrder = g.Max(o => o.CreatedAt)
                })
                .ToList();

            var newCustomers = customerOrders.Count(c => c.FirstOrder >= startDate);
            var activeCustomers = customerOrders.Count(c => c.LastOrder >= startDate.AddDays(-30));
            var totalCustomers = customers.Count;

            var segments = new List<CustomerSegmentDto>
            {
                new CustomerSegmentDto
                {
                    SegmentName = "New",
                    CustomerCount = newCustomers,
                    TotalRevenue = customerOrders.Where(c => c.FirstOrder >= startDate).Sum(c => c.TotalSpent),
                    AverageOrderValue = newCustomers > 0 ? customerOrders.Where(c => c.FirstOrder >= startDate).Average(c => c.TotalSpent / c.OrderCount) : 0
                },
                new CustomerSegmentDto
                {
                    SegmentName = "Returning",
                    CustomerCount = customerOrders.Count - newCustomers,
                    TotalRevenue = customerOrders.Where(c => c.FirstOrder < startDate).Sum(c => c.TotalSpent),
                    AverageOrderValue = customerOrders.Count > newCustomers ? customerOrders.Where(c => c.FirstOrder < startDate).Average(c => c.TotalSpent / c.OrderCount) : 0
                },
                new CustomerSegmentDto
                {
                    SegmentName = "VIP",
                    CustomerCount = customerOrders.Count(c => c.TotalSpent > 1000),
                    TotalRevenue = customerOrders.Where(c => c.TotalSpent > 1000).Sum(c => c.TotalSpent),
                    AverageOrderValue = customerOrders.Count(c => c.TotalSpent > 1000) > 0 ? customerOrders.Where(c => c.TotalSpent > 1000).Average(c => c.TotalSpent / c.OrderCount) : 0
                }
            };

            return new CustomerReportDto
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalCustomers = totalCustomers,
                NewCustomers = newCustomers,
                ActiveCustomers = activeCustomers,
                AverageLifetimeValue = customerOrders.Any() ? customerOrders.Average(c => c.TotalSpent) : 0,
                RepeatPurchaseRate = customerOrders.Count > 0 ? (double)customerOrders.Count(c => c.OrderCount > 1) / customerOrders.Count : 0,
                Segments = segments
            };
        }
    }

    public class ExportReportQueryHandler : IQueryHandler<ExportReportQuery, ExportResult>
    {
        private readonly IApplicationDbContext _db;

        public ExportReportQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ExportResult> Handle(ExportReportQuery query, CancellationToken cancellationToken = default)
        {
            var content = query.ReportType switch
            {
                "sales" => GenerateSalesCsv(query.Parameters),
                "revenue" => GenerateRevenueCsv(query.Parameters),
                "inventory" => GenerateInventoryCsv(query.Parameters),
                "customer" => GenerateCustomerCsv(query.Parameters),
                _ => "Report type not supported"
            };

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);

            return new ExportResult
            {
                Content = bytes,
                ContentType = query.Parameters.Format == "csv" ? "text/csv" : "application/json",
                FileName = $"{query.ReportType}_report_{DateTimeOffset.UtcNow:yyyyMMdd}.{query.Parameters.Format}"
            };
        }

        private string GenerateSalesCsv(ReportParameters p)
        {
            return "Period,Orders,Revenue,NewCustomers\n";
        }

        private string GenerateRevenueCsv(ReportParameters p)
        {
            return "Period,GrossRevenue,NetRevenue,Discounts,Refunds\n";
        }

        private string GenerateInventoryCsv(ReportParameters p)
        {
            return "Warehouse,Products,Variants,Value,LowStock\n";
        }

        private string GenerateCustomerCsv(ReportParameters p)
        {
            return "Segment,Customers,Revenue,AvgOrderValue\n";
        }
    }
}