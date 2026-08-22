using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminReportingHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private AutoMapper.IMapper CreateMapper()
        {
            return new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        private static void SetPrivateProperty(object target, string propertyName, object? value)
        {
            var prop = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(target, value);
        }

        private static async Task<Order> CreateOrderAsync(
            ApplicationDbContext ctx,
            Guid userId,
            List<OrderItem> items,
            OrderStatus status = OrderStatus.Completed,
            decimal discount = 0,
            decimal shipping = 0,
            DateTimeOffset? createdAt = null)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{Guid.NewGuid().ToString()[..8]}",
                UserId = userId,
                ShippingAmount = shipping
            };

            foreach (var item in items)
            {
                order.AddItem(item.ProductId, item.ProductVariantId, item.ProductName, item.UnitPrice, item.Quantity, item.DiscountAmount);
            }

            order.PlaceOrder();

            if (discount > 0)
            {
                order.ApplyCoupon("TESTDISCOUNT", discount);
            }

            SetPrivateProperty(order, "Status", status);

            if (createdAt.HasValue)
            {
                SetPrivateProperty(order, "CreatedAt", createdAt.Value);
                SetPrivateProperty(order, "UpdatedAt", createdAt.Value);
            }

            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();
            return order;
        }

        [Fact]
        public async Task GetSalesReport_ReturnsAggregatedData_AndCalculatesTopProductsAndCategories()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Laptops & Computers",
                Slug = "laptops",
                IsActive = true
            };
            await ctx.Categories.AddAsync(category);

            var product1 = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Dell XPS 15",
                Slug = "dell-xps-15",
                Sku = "DELL-001",
                BasePrice = 1500m,
                Status = "Active",
                IsActive = true,
                CategoryId = category.Id
            };
            var product2 = new Product
            {
                Id = Guid.NewGuid(),
                Name = "MacBook Pro 16",
                Slug = "macbook-pro-16",
                Sku = "APPL-001",
                BasePrice = 2500m,
                Status = "Active",
                IsActive = true,
                CategoryId = category.Id
            };
            await ctx.Products.AddRangeAsync(product1, product2);

            var customer1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user1@test.com",
                Email = "user1@test.com",
                FirstName = "Mohammad",
                LastName = "Sofan"
            };
            var customer2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user2@test.com",
                Email = "user2@test.com",
                FirstName = "Ahmad",
                LastName = "Ali"
            };
            await ctx.Set<ApplicationUser>().AddRangeAsync(customer1, customer2);

            // Create orders
            await CreateOrderAsync(ctx, customer1.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = product1.Id, ProductName = product1.Name, Quantity = 2, UnitPrice = 1500m, TotalAmount = 3000m }
            });

            await CreateOrderAsync(ctx, customer2.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = product2.Id, ProductName = product2.Name, Quantity = 1, UnitPrice = 2500m, TotalAmount = 2500m }
            });

            // Cancelled order should not be counted
            await CreateOrderAsync(ctx, customer1.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = product1.Id, ProductName = product1.Name, Quantity = 5, UnitPrice = 1500m, TotalAmount = 7500m }
            }, status: OrderStatus.Cancelled);

            var handler = new GetSalesReportQueryHandler(ctx, mapper);
            var result = await handler.Handle(new GetSalesReportQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-7),
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                GroupBy = "day"
            });

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalOrders);
            Assert.Equal(5500m, result.TotalRevenue);
            Assert.Equal(2750m, result.AverageOrderValue);
            Assert.Equal(2, result.TopProducts.Count);

            // Dell XPS had quantity 2 sold, MacBook had quantity 1 sold
            Assert.Equal("Dell XPS 15", result.TopProducts[0].ProductName);
            Assert.Equal(2, result.TopProducts[0].TotalSold);
            Assert.Equal(3000m, result.TopProducts[0].Revenue);

            Assert.Single(result.TopCategories);
            Assert.Equal("Laptops & Computers", result.TopCategories[0].CategoryName);
            Assert.Equal(5500m, result.TopCategories[0].Revenue);
        }

        [Fact]
        public async Task GetSalesReport_GroupsByWeekAndMonthCorrectly()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();

            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var pastDate = DateTimeOffset.UtcNow.AddDays(-14);
            var recentDate = DateTimeOffset.UtcNow;

            await CreateOrderAsync(ctx, customerId, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Item 1", Quantity = 1, UnitPrice = 100m, TotalAmount = 100m }
            }, createdAt: pastDate);

            await CreateOrderAsync(ctx, customerId, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Item 1", Quantity = 2, UnitPrice = 100m, TotalAmount = 200m }
            }, createdAt: recentDate);

            var handler = new GetSalesReportQueryHandler(ctx, mapper);

            var weekReport = await handler.Handle(new GetSalesReportQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                GroupBy = "week"
            });
            Assert.NotNull(weekReport);
            Assert.True(weekReport.SalesByPeriod.Count >= 1);

            var monthReport = await handler.Handle(new GetSalesReportQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                GroupBy = "month"
            });
            Assert.NotNull(monthReport);
            Assert.True(monthReport.SalesByPeriod.Count >= 1);
        }

        [Fact]
        public async Task GetRevenueReport_CalculatesGrossNetDiscountsRefundsTaxAndShipping()
        {
            using var ctx = CreateInMemoryContext();
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            // Order 1: 500 total, 50 discount, 20 shipping
            var order1 = await CreateOrderAsync(ctx, customerId, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Product A", Quantity = 5, UnitPrice = 100m, TotalAmount = 500m }
            }, discount: 50m, shipping: 20m);

            // Order 2: 300 total, 0 discount, 10 shipping
            var order2 = await CreateOrderAsync(ctx, customerId, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Product B", Quantity = 3, UnitPrice = 100m, TotalAmount = 300m }
            }, discount: 0m, shipping: 10m);

            // Refund on Order 1: 100 refund
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = Guid.NewGuid(),
                Amount = 100m,
                Reason = "Customer return",
                Status = "succeeded",
                ProcessedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Refunds.AddAsync(refund);
            await ctx.SaveChangesAsync();

            var handler = new GetRevenueReportQueryHandler(ctx);
            var report = await handler.Handle(new GetRevenueReportQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-7),
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                GroupBy = "day"
            });

            Assert.NotNull(report);
            Assert.Equal(780m, report.GrossRevenue);
            Assert.Equal(680m, report.NetRevenue); // 780 - 100 refund = 680
            Assert.Equal(100m, report.TotalRefunds);
            Assert.Equal(50m, report.TotalDiscounts);
            Assert.Equal(30m, report.TotalShipping);
            Assert.NotEmpty(report.RevenueByChannel);
            Assert.Equal(780m, report.RevenueByChannel[0].Revenue);
        }

        [Fact]
        public async Task GetInventoryReport_ReturnsWarehouseAndCategoryBreakdown_AndStockAlerts()
        {
            using var ctx = CreateInMemoryContext();

            var warehouse1 = new Warehouse { Id = Guid.NewGuid(), Name = "Main Hub Amman", Code = "AMM-01", IsActive = true };
            var warehouse2 = new Warehouse { Id = Guid.NewGuid(), Name = "Zarqa Depot", Code = "ZRQ-01", IsActive = true };
            await ctx.Warehouses.AddRangeAsync(warehouse1, warehouse2);

            var category = new Category { Id = Guid.NewGuid(), Name = "Smartphones", Slug = "smartphones", IsActive = true };
            await ctx.Categories.AddAsync(category);

            var product1 = new Product
            {
                Id = Guid.NewGuid(),
                Name = "iPhone 15 Pro",
                Slug = "iphone-15-pro",
                Sku = "IPH-15",
                BasePrice = 1200m,
                Status = "Active",
                IsActive = true,
                CategoryId = category.Id
            };
            var product2 = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Galaxy S24 Ultra",
                Slug = "s24-ultra",
                Sku = "GAL-S24",
                BasePrice = 1100m,
                Status = "Active",
                IsActive = true,
                CategoryId = category.Id
            };
            await ctx.Products.AddRangeAsync(product1, product2);

            // inv1: 20 available (normal stock)
            var inv1 = new InventoryItem { Id = Guid.NewGuid(), ProductId = product1.Id, WarehouseId = warehouse1.Id, ReorderLevel = 10 };
            inv1.AddStock(20);

            // inv2: 5 available (low stock <= 10)
            var inv2 = new InventoryItem { Id = Guid.NewGuid(), ProductId = product2.Id, WarehouseId = warehouse1.Id, ReorderLevel = 10 };
            inv2.AddStock(5);

            // inv3: 0 available (out of stock)
            var inv3 = new InventoryItem { Id = Guid.NewGuid(), ProductId = product1.Id, WarehouseId = warehouse2.Id, ReorderLevel = 5 };

            await ctx.InventoryItems.AddRangeAsync(inv1, inv2, inv3);
            await ctx.SaveChangesAsync();

            var handler = new GetInventoryReportQueryHandler(ctx);
            var report = await handler.Handle(new GetInventoryReportQuery
            {
                AsOfDate = DateTimeOffset.UtcNow
            });

            Assert.NotNull(report);
            Assert.Equal(2, report.TotalProducts);
            Assert.Equal(1, report.LowStockCount); // inv2 has 5
            Assert.Equal(1, report.OutOfStockCount); // inv3 has 0
            Assert.Equal((20 * 1200m) + (5 * 1100m), report.TotalInventoryValue); // 24000 + 5500 = 29500

            Assert.Equal(2, report.ByWarehouse.Count);
            Assert.Single(report.ByCategory);
            Assert.Equal("Smartphones", report.ByCategory[0].CategoryName);
            Assert.Equal(29500m, report.ByCategory[0].TotalValue);
        }

        [Fact]
        public async Task GetCustomerReport_CalculatesAcquisitionMetrics_AndSegments()
        {
            using var ctx = CreateInMemoryContext();

            var customer1 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "new@test.com", Email = "new@test.com", FirstName = "New", LastName = "User" };
            var customer2 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "vip@test.com", Email = "vip@test.com", FirstName = "VIP", LastName = "User" };
            await ctx.Set<ApplicationUser>().AddRangeAsync(customer1, customer2);

            var productId = Guid.NewGuid();

            // Customer 1: New order of 100
            await CreateOrderAsync(ctx, customer1.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Item", Quantity = 1, UnitPrice = 100m, TotalAmount = 100m }
            });

            // Customer 2: VIP high spender (1500 total)
            await CreateOrderAsync(ctx, customer2.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Item", Quantity = 1, UnitPrice = 1500m, TotalAmount = 1500m }
            });

            var handler = new GetCustomerReportQueryHandler(ctx);
            var report = await handler.Handle(new GetCustomerReportQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow.AddDays(1)
            });

            Assert.NotNull(report);
            Assert.Equal(2, report.TotalCustomers);
            Assert.Equal(2, report.NewCustomers);
            Assert.Equal(2, report.ActiveCustomers);
            Assert.Equal(800m, report.AverageLifetimeValue); // (100 + 1500) / 2 = 800

            var vipSegment = report.Segments.FirstOrDefault(s => s.SegmentName == "VIP");
            Assert.NotNull(vipSegment);
            Assert.Equal(1, vipSegment.CustomerCount);
            Assert.Equal(1500m, vipSegment.TotalRevenue);
        }

        [Fact]
        public async Task ExportReport_GeneratesCsvAndJsonExports_ForReports()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new ExportReportQueryHandler(ctx);

            var salesExport = await handler.Handle(new ExportReportQuery
            {
                ReportType = "sales",
                Parameters = new ReportParameters { Format = "csv" }
            });
            Assert.NotNull(salesExport);
            Assert.Equal("text/csv", salesExport.ContentType);
            Assert.Contains(".csv", salesExport.FileName);
            Assert.NotEmpty(salesExport.Content);

            var revenueExport = await handler.Handle(new ExportReportQuery
            {
                ReportType = "revenue",
                Parameters = new ReportParameters { Format = "json" }
            });
            Assert.NotNull(revenueExport);
            Assert.Equal("application/json", revenueExport.ContentType);
            Assert.Contains(".json", revenueExport.FileName);
            Assert.NotEmpty(revenueExport.Content);

            var inventoryExport = await handler.Handle(new ExportReportQuery
            {
                ReportType = "inventory",
                Parameters = new ReportParameters { Format = "csv" }
            });
            Assert.NotNull(inventoryExport);
            Assert.Equal("text/csv", inventoryExport.ContentType);

            var customerExport = await handler.Handle(new ExportReportQuery
            {
                ReportType = "customer",
                Parameters = new ReportParameters { Format = "csv" }
            });
            Assert.NotNull(customerExport);
            Assert.Equal("text/csv", customerExport.ContentType);
        }
    }
}

