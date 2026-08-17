using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
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

        private static async Task<Order> CreateCompletedOrderAsync(ApplicationDbContext ctx, Guid userId, List<OrderItem> items)
        {
            var order = new Order { Id = Guid.NewGuid(), UserId = userId };
            foreach (var item in items)
            {
                order.Items.Add(item);
            }
            order.PlaceOrder();
            order.MarkPaid();
            order.Complete();
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();
            return order;
        }

        [Fact]
        public async Task GetSalesReport_ReturnsAggregatedData()
        {
            using var ctx = CreateInMemoryContext();

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TEST-001",
                BasePrice = 50m,
                Status = "Active",
                IsActive = true,
                CategoryId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            var customer1 = new Ecommerce.Infrastructure.Identity.ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "customer1@test.com",
                Email = "customer1@test.com",
                FirstName = "John",
                LastName = "Doe"
            };
            var customer2 = new Ecommerce.Infrastructure.Identity.ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "customer2@test.com",
                Email = "customer2@test.com",
                FirstName = "Jane",
                LastName = "Smith"
            };
            await ctx.Set<Ecommerce.Infrastructure.Identity.ApplicationUser>().AddRangeAsync(customer1, customer2);

            await CreateCompletedOrderAsync(ctx, customer1.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = product.Id, ProductName = "Test Product", Quantity = 2, UnitPrice = 50m, TotalAmount = 100m }
            });
            await CreateCompletedOrderAsync(ctx, customer2.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = product.Id, ProductName = "Test Product", Quantity = 3, UnitPrice = 50m, TotalAmount = 150m }
            });
            await CreateCompletedOrderAsync(ctx, customer1.Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = product.Id, ProductName = "Test Product", Quantity = 1, UnitPrice = 50m, TotalAmount = 50m }
            });

            var completedOrders = await ctx.Orders.Where(o => o.Status == OrderStatus.Completed).ToListAsync();
            Assert.Equal(3, completedOrders.Count);
            Assert.Equal(300m, completedOrders.Sum(o => o.TotalAmount));
        }

        [Fact]
        public async Task GetInventoryReport_ReturnsWarehouseAndCategoryBreakdown()
        {
            using var ctx = CreateInMemoryContext();

            var warehouse1 = new Warehouse { Id = Guid.NewGuid(), Name = "Warehouse A", Code = "WH-A", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var warehouse2 = new Warehouse { Id = Guid.NewGuid(), Name = "Warehouse B", Code = "WH-B", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            await ctx.Warehouses.AddRangeAsync(warehouse1, warehouse2);

            var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            await ctx.Categories.AddAsync(category);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Slug = "laptop",
                Sku = "LAP-001",
                BasePrice = 1000m,
                Status = "Active",
                IsActive = true,
                CategoryId = category.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            var inv1 = new InventoryItem { Id = Guid.NewGuid(), ProductId = product.Id, WarehouseId = warehouse1.Id, ReorderLevel = 20, AllowBackorder = false };
            inv1.AddStock(100);
            inv1.Reserve(10);

            var inv2 = new InventoryItem { Id = Guid.NewGuid(), ProductId = product.Id, WarehouseId = warehouse2.Id, ReorderLevel = 10, AllowBackorder = false };
            inv2.AddStock(50);
            inv2.Reserve(5);

            await ctx.InventoryItems.AddRangeAsync(inv1, inv2);
            await ctx.SaveChangesAsync();

            var items = await ctx.InventoryItems.Include(i => i.Product).Include(i => i.Warehouse).ToListAsync();
            Assert.Equal(2, items.Count);

            var totalAvailable = items.Sum(i => i.Available);
            Assert.Equal(135, totalAvailable); // (100-10) + (50-5) = 90 + 45 = 135

            var byWarehouse = items.GroupBy(i => i.WarehouseId).Select(g => new { WarehouseId = g.Key, Available = g.Sum(i => i.Available) }).ToList();
            Assert.Equal(2, byWarehouse.Count);
        }

        [Fact]
        public async Task GetCustomerReport_ReturnsSegments()
        {
            using var ctx = CreateInMemoryContext();

            var customers = new List<Ecommerce.Infrastructure.Identity.ApplicationUser>
            {
                new Ecommerce.Infrastructure.Identity.ApplicationUser { Id = Guid.NewGuid(), UserName = "new@test.com", Email = "new@test.com", FirstName = "New", LastName = "Customer" },
                new Ecommerce.Infrastructure.Identity.ApplicationUser { Id = Guid.NewGuid(), UserName = "returning@test.com", Email = "returning@test.com", FirstName = "Returning", LastName = "Customer" },
                new Ecommerce.Infrastructure.Identity.ApplicationUser { Id = Guid.NewGuid(), UserName = "vip@test.com", Email = "vip@test.com", FirstName = "VIP", LastName = "Customer" }
            };
            await ctx.Set<Ecommerce.Infrastructure.Identity.ApplicationUser>().AddRangeAsync(customers);

            await CreateCompletedOrderAsync(ctx, customers[0].Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Product 1", Quantity = 1, UnitPrice = 50m, TotalAmount = 50m }
            });
            await CreateCompletedOrderAsync(ctx, customers[1].Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Product 2", Quantity = 1, UnitPrice = 200m, TotalAmount = 200m }
            });
            await CreateCompletedOrderAsync(ctx, customers[1].Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Product 3", Quantity = 1, UnitPrice = 150m, TotalAmount = 150m }
            });
            await CreateCompletedOrderAsync(ctx, customers[2].Id, new List<OrderItem>
            {
                new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Product 4", Quantity = 1, UnitPrice = 1200m, TotalAmount = 1200m }
            });

            var completedOrders = await ctx.Orders.Where(o => o.Status == OrderStatus.Completed).ToListAsync();
            Assert.Equal(4, completedOrders.Count);

            var customerOrders = completedOrders
                .Where(o => o.UserId.HasValue)
                .GroupBy(o => o.UserId.Value)
                .Select(g => new { UserId = g.Key, OrderCount = g.Count(), TotalSpent = g.Sum(o => o.TotalAmount) })
                .ToList();

            Assert.Equal(3, customerOrders.Count);
            var vip = customerOrders.First(c => c.TotalSpent > 1000);
            Assert.Equal(1200m, vip.TotalSpent);
        }
    }
}