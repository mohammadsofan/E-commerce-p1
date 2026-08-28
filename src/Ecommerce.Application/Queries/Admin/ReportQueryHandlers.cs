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
    /// <summary>
    /// Shared date-range normalisation so an inverted range is rejected consistently
    /// instead of quietly returning zeroed metrics.
    /// </summary>
    internal static class ReportPeriod
    {
        public static string ValidateGroupBy(string? groupBy)
        {
            var normalized = string.IsNullOrWhiteSpace(groupBy) ? "day" : groupBy.Trim().ToLowerInvariant();
            if (normalized is not ("day" or "week" or "month"))
                throw new Ecommerce.Domain.Exceptions.DomainException("تجميع التقرير غير مدعوم. القيم المتاحة: day, week, month.");
            return normalized;
        }

        public static (DateTimeOffset Start, DateTimeOffset End) Resolve(
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            int defaultDays = 30)
        {
            var start = startDate?.Date ?? DateTimeOffset.UtcNow.AddDays(-defaultDays);
            var end = endDate.HasValue
                ? endDate.Value.Date.AddDays(1).AddTicks(-1)
                : DateTimeOffset.UtcNow;

            if (end < start)
                throw new Ecommerce.Domain.Exceptions.DomainException("تاريخ النهاية يجب أن يكون بعد تاريخ البداية.");

            return (start, end);
        }
    }

    public class GetSalesReportQueryHandler : IQueryHandler<GetSalesReportQuery, SalesReportDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper? _mapper;

        public GetSalesReportQueryHandler(IApplicationDbContext db, IMapper? mapper = null)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<SalesReportDto> Handle(GetSalesReportQuery query, CancellationToken cancellationToken = default)
        {
            query.GroupBy = ReportPeriod.ValidateGroupBy(query.GroupBy);
            var (effectiveStart, effectiveEnd) = ReportPeriod.Resolve(query.StartDate, query.EndDate);

            var ordersQuery = _db.Orders
                .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled
                         && o.CreatedAt >= effectiveStart && o.CreatedAt <= effectiveEnd);

            var orders = await ordersQuery.ToListAsync(cancellationToken);

            var orderIds = orders.Select(o => o.Id).ToList();
            var orderItems = orderIds.Any()
                ? await _db.OrderItems
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .ToListAsync(cancellationToken)
                : new List<OrderItem>();

            var customerIds = orders.Where(o => o.UserId.HasValue).Select(o => o.UserId!.Value).Distinct().ToList();
            var newCustomerIds = await _db.Orders
                .Where(o => o.UserId.HasValue && customerIds.Contains(o.UserId.Value))
                .GroupBy(o => o.UserId!.Value)
                .Where(g => g.Min(o => o.CreatedAt) >= effectiveStart)
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
            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var productCategoryMap = products.ToDictionary(
                p => p.Id,
                p => new { CategoryId = p.CategoryId, CategoryName = p.Category?.Name ?? "General" }
            );

            var topCategories = orderItems
                .Where(oi => productCategoryMap.ContainsKey(oi.ProductId) && productCategoryMap[oi.ProductId].CategoryId.HasValue)
                .GroupBy(oi => productCategoryMap[oi.ProductId].CategoryId!.Value)
                .Select(g => new TopCategoryDto
                {
                    CategoryId = g.Key,
                    CategoryName = productCategoryMap.Values.FirstOrDefault(c => c.CategoryId == g.Key)?.CategoryName ?? "General",
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    Revenue = g.Sum(oi => oi.TotalAmount)
                })
                .OrderByDescending(c => c.Revenue)
                .Take(10)
                .ToList();

            return new SalesReportDto
            {
                PeriodStart = effectiveStart,
                PeriodEnd = effectiveEnd,
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
            return groupBy?.ToLowerInvariant() switch
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
            query.GroupBy = ReportPeriod.ValidateGroupBy(query.GroupBy);
            var (effectiveStart, effectiveEnd) = ReportPeriod.Resolve(query.StartDate, query.EndDate);

            var ordersQuery = _db.Orders
                .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled
                         && o.CreatedAt >= effectiveStart && o.CreatedAt <= effectiveEnd);

            var refundsQuery = _db.Refunds
                .Where(r => r.Status == "succeeded" && r.ProcessedAt >= effectiveStart && r.ProcessedAt <= effectiveEnd);

            var orders = await ordersQuery.ToListAsync(cancellationToken);
            var refunds = await refundsQuery.ToListAsync(cancellationToken);

            var totalDiscounts = orders.Sum(o => o.DiscountAmount);
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
                .Where(r => r.ProcessedAt.HasValue)
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
                PeriodStart = effectiveStart,
                PeriodEnd = effectiveEnd,
                GrossRevenue = grossRevenue,
                NetRevenue = grossRevenue - totalRefunds,
                TotalDiscounts = totalDiscounts,
                TotalRefunds = totalRefunds,
                TotalShipping = totalShipping,
                RevenueByPeriod = revenueByPeriod,
                RevenueByChannel = revenueByChannel
            };
        }

        private string GetPeriodKey(DateTimeOffset date, string groupBy)
        {
            return groupBy?.ToLowerInvariant() switch
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
            var (effectiveStart, effectiveEnd) = ReportPeriod.Resolve(query.StartDate, query.EndDate);

            var ordersQuery = _db.Orders
                .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled
                         && o.CreatedAt >= effectiveStart && o.CreatedAt <= effectiveEnd);

            var totalUsersCount = await _db.Users.CountAsync(cancellationToken);
            var orders = await ordersQuery.ToListAsync(cancellationToken);

            var customerOrders = orders
                .Where(o => o.UserId.HasValue)
                .GroupBy(o => o.UserId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    FirstOrder = g.Min(o => o.CreatedAt),
                    LastOrder = g.Max(o => o.CreatedAt)
                })
                .ToList();

            var newCustomers = customerOrders.Count(c => c.FirstOrder >= effectiveStart);
            var activeCustomers = customerOrders.Count;
            var totalCustomers = Math.Max(totalUsersCount, customerOrders.Count);

            var segments = new List<CustomerSegmentDto>
            {
                new CustomerSegmentDto
                {
                    SegmentName = "New",
                    CustomerCount = newCustomers,
                    TotalRevenue = customerOrders.Where(c => c.FirstOrder >= effectiveStart).Sum(c => c.TotalSpent),
                    AverageOrderValue = newCustomers > 0 ? customerOrders.Where(c => c.FirstOrder >= effectiveStart).Average(c => c.TotalSpent / c.OrderCount) : 0
                },
                new CustomerSegmentDto
                {
                    SegmentName = "Returning",
                    CustomerCount = customerOrders.Count - newCustomers,
                    TotalRevenue = customerOrders.Where(c => c.FirstOrder < effectiveStart).Sum(c => c.TotalSpent),
                    AverageOrderValue = customerOrders.Count > newCustomers ? customerOrders.Where(c => c.FirstOrder < effectiveStart).Average(c => c.TotalSpent / c.OrderCount) : 0
                },
                new CustomerSegmentDto
                {
                    SegmentName = "VIP",
                    CustomerCount = customerOrders.Count(c => c.TotalSpent >= 500),
                    TotalRevenue = customerOrders.Where(c => c.TotalSpent >= 500).Sum(c => c.TotalSpent),
                    AverageOrderValue = customerOrders.Count(c => c.TotalSpent >= 500) > 0 ? customerOrders.Where(c => c.TotalSpent >= 500).Average(c => c.TotalSpent / c.OrderCount) : 0
                }
            };

            return new CustomerReportDto
            {
                PeriodStart = effectiveStart,
                PeriodEnd = effectiveEnd,
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
        private readonly IMapper? _mapper;

        public ExportReportQueryHandler(IApplicationDbContext db, IMapper? mapper = null)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ExportResult> Handle(ExportReportQuery query, CancellationToken cancellationToken = default)
        {
            var isJson = string.Equals(query.Parameters.Format, "json", StringComparison.OrdinalIgnoreCase);
            var reportType = query.ReportType?.Trim().ToLowerInvariant() ?? string.Empty;

            // Only known report types are accepted, and the filename is derived from the
            // validated value so the caller cannot inject header/path content into it.
            if (!ExportFileNames.TryGetValue(reportType, out var fileStem))
            {
                throw new Ecommerce.Domain.Exceptions.DomainException(
                    $"نوع التقرير غير مدعوم. الأنواع المتاحة: {string.Join(", ", ExportFileNames.Keys)}.");
            }

            string content;

            switch (reportType)
            {
                case "sales":
                {
                    var salesHandler = new GetSalesReportQueryHandler(_db, _mapper);
                    var salesQuery = new GetSalesReportQuery
                    {
                        StartDate = query.Parameters.StartDate,
                        EndDate = query.Parameters.EndDate,
                        GroupBy = query.Parameters.GroupBy
                    };
                    var salesReport = await salesHandler.Handle(salesQuery, cancellationToken);
                    if (isJson)
                    {
                        content = System.Text.Json.JsonSerializer.Serialize(salesReport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Period,Orders,Revenue,NewCustomers");
                        foreach (var row in salesReport.SalesByPeriod)
                        {
                            sb.AppendLine($"{EscapeCsv(row.Period)},{row.OrderCount},{row.Revenue:F2},{row.NewCustomers}");
                        }
                        content = sb.ToString();
                    }
                    break;
                }
                case "revenue":
                {
                    var revenueHandler = new GetRevenueReportQueryHandler(_db);
                    var revenueQuery = new GetRevenueReportQuery
                    {
                        StartDate = query.Parameters.StartDate,
                        EndDate = query.Parameters.EndDate,
                        GroupBy = query.Parameters.GroupBy
                    };
                    var revenueReport = await revenueHandler.Handle(revenueQuery, cancellationToken);
                    if (isJson)
                    {
                        content = System.Text.Json.JsonSerializer.Serialize(revenueReport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Period,GrossRevenue,NetRevenue,Discounts,Refunds");
                        foreach (var row in revenueReport.RevenueByPeriod)
                        {
                            sb.AppendLine($"{EscapeCsv(row.Period)},{row.GrossRevenue:F2},{row.NetRevenue:F2},{row.Discounts:F2},{row.Refunds:F2}");
                        }
                        content = sb.ToString();
                    }
                    break;
                }
                case "inventory":
                {
                    var inventoryHandler = new GetInventoryReportQueryHandler(_db);
                    var inventoryQuery = new GetInventoryReportQuery
                    {
                        AsOfDate = query.Parameters.StartDate,
                        WarehouseIds = query.Parameters.WarehouseIds ?? new List<Guid>(),
                        CategoryIds = query.Parameters.CategoryIds ?? new List<Guid>()
                    };
                    var inventoryReport = await inventoryHandler.Handle(inventoryQuery, cancellationToken);
                    if (isJson)
                    {
                        content = System.Text.Json.JsonSerializer.Serialize(inventoryReport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Warehouse,Products,Variants,Value,LowStock");
                        foreach (var row in inventoryReport.ByWarehouse)
                        {
                            sb.AppendLine($"{EscapeCsv(row.WarehouseName)},{row.ProductCount},{row.VariantCount},{row.TotalValue:F2},{row.LowStockCount}");
                        }
                        content = sb.ToString();
                    }
                    break;
                }
                case "customer":
                case "customers":
                {
                    var customerHandler = new GetCustomerReportQueryHandler(_db);
                    var customerQuery = new GetCustomerReportQuery
                    {
                        StartDate = query.Parameters.StartDate,
                        EndDate = query.Parameters.EndDate
                    };
                    var customerReport = await customerHandler.Handle(customerQuery, cancellationToken);
                    if (isJson)
                    {
                        content = System.Text.Json.JsonSerializer.Serialize(customerReport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Segment,Customers,Revenue,AvgOrderValue");
                        foreach (var row in customerReport.Segments)
                        {
                            sb.AppendLine($"{EscapeCsv(row.SegmentName)},{row.CustomerCount},{row.TotalRevenue:F2},{row.AverageOrderValue:F2}");
                        }
                        content = sb.ToString();
                    }
                    break;
                }
                default:
                    // Unreachable: unknown types are rejected above.
                    throw new Ecommerce.Domain.Exceptions.DomainException("نوع التقرير غير مدعوم.");
            }

            byte[] bytes;
            if (isJson)
            {
                bytes = System.Text.Encoding.UTF8.GetBytes(content);
            }
            else
            {
                var preamble = System.Text.Encoding.UTF8.GetPreamble();
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                bytes = new byte[preamble.Length + contentBytes.Length];
                Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
                Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
            }
            var format = isJson ? "json" : "csv";

            return new ExportResult
            {
                Content = bytes,
                ContentType = isJson ? "application/json" : "text/csv",
                FileName = $"{fileStem}_report_{DateTimeOffset.UtcNow:yyyyMMdd}.{format}"
            };
        }

        /// <summary>
        /// Allow-list of exportable reports mapped to the safe filename stem used in
        /// Content-Disposition.
        /// </summary>
        private static readonly Dictionary<string, string> ExportFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sales"] = "sales",
            ["revenue"] = "revenue",
            ["inventory"] = "inventory",
            ["customer"] = "customers",
            ["customers"] = "customers"
        };

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
