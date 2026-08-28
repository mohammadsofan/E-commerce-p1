using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Mappings;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class Phase6AdministrationAndWorkerTests
    {
        private ApplicationDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
                cfg.AddProfile<Ecommerce.Infrastructure.Mappings.AdminUserMappingProfile>();
            });
            return config.CreateMapper();
        }

        private UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext ctx)
        {
            var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(ctx);
            var options = Options.Create(new IdentityOptions());
            var pwdHasher = new PasswordHasher<ApplicationUser>();
            var userValidators = new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() };
            var pwdValidators = new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() };
            var normalizer = new UpperInvariantLookupNormalizer();
            var errorDescriber = new IdentityErrorDescriber();
            var logger = new NullLogger<UserManager<ApplicationUser>>();

            return new UserManager<ApplicationUser>(store, options, pwdHasher, userValidators, pwdValidators, normalizer, errorDescriber, null, logger);
        }

        private static void SetPrivateProperty(object target, string propertyName, object? value)
        {
            var prop = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(target, value);
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId { get; set; }
            public string? UserName { get; set; } = "admin-user";
            public bool IsAdmin { get; set; } = true;

            public FakeCurrentUserService(Guid? userId = null, bool isAdmin = true)
            {
                UserId = userId;
                IsAdmin = isAdmin;
            }
        }

        private sealed class FakeRefreshTokenService : IRefreshTokenService
        {
            public Task<(string Token, DateTimeOffset ExpiresAt)> CreateRefreshTokenAsync(Guid userId) =>
                Task.FromResult(("fake-token", DateTimeOffset.UtcNow.AddDays(7)));

            public Task<(bool Success, string? AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt)> RefreshAsync(string refreshToken) =>
                Task.FromResult((true, (string?)"access", (string?)"refresh", (DateTimeOffset?)DateTimeOffset.UtcNow.AddDays(7)));

            public Task<bool> RevokeAsync(string refreshToken) => Task.FromResult(true);

            public Task<bool> RevokeAllAsync(Guid userId) => Task.FromResult(true);

            public Task<int> RemoveExpiredAsync() => Task.FromResult(0);
        }

        // =========================================================================
        // Issue 1: AbandonedOrderCleanupService
        // =========================================================================
        [Fact]
        public async Task AbandonedOrderCleanupService_CancelsStalePlacedOrders_AndReleasesInventory()
        {
            var dbName = Guid.NewGuid().ToString();
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var invId = Guid.NewGuid();
            var staleOrderId = Guid.NewGuid();
            var recentOrderId = Guid.NewGuid();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var invItem = new InventoryItem(productId, warehouseId, quantityOnHand: 20);
                invItem.Id = invId;
                invItem.Reserve(5);
                Assert.Equal(15, invItem.Available);
                Assert.Equal(5, invItem.QuantityReserved);
                await ctx.InventoryItems.AddAsync(invItem);

                // Stale order placed 40 mins ago
                var staleOrder = new Order
                {
                    Id = staleOrderId,
                    OrderNumber = "ORD-STALE-001"
                };
                staleOrder.AddItem(productId, Guid.Empty, "Test Product", 50m, 5);
                SetPrivateProperty(staleOrder, "Status", OrderStatus.Placed);
                SetPrivateProperty(staleOrder, "CreatedAt", DateTimeOffset.UtcNow.AddMinutes(-40));
                await ctx.Orders.AddAsync(staleOrder);

                // Active order placed 5 mins ago
                var recentOrder = new Order
                {
                    Id = recentOrderId,
                    OrderNumber = "ORD-RECENT-001"
                };
                recentOrder.AddItem(productId, Guid.Empty, "Test Product", 50m, 2);
                SetPrivateProperty(recentOrder, "Status", OrderStatus.Placed);
                SetPrivateProperty(recentOrder, "CreatedAt", DateTimeOffset.UtcNow.AddMinutes(-5));
                await ctx.Orders.AddAsync(recentOrder);

                await ctx.SaveChangesAsync();
            }

            // Set up service provider
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            var provider = services.BuildServiceProvider();

            var cleanupService = new AbandonedOrderCleanupService(
                provider,
                NullLogger<AbandonedOrderCleanupService>.Instance,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30));

            var cleanedCount = await cleanupService.CleanupAbandonedOrdersAsync(CancellationToken.None);

            Assert.Equal(1, cleanedCount);

            using (var verifyCtx = CreateInMemoryContext(dbName))
            {
                var updatedStaleOrder = await verifyCtx.Orders.FindAsync(staleOrderId);
                Assert.NotNull(updatedStaleOrder);
                Assert.Equal(OrderStatus.Cancelled, updatedStaleOrder.Status);

                var updatedRecentOrder = await verifyCtx.Orders.FindAsync(recentOrderId);
                Assert.NotNull(updatedRecentOrder);
                Assert.Equal(OrderStatus.Placed, updatedRecentOrder.Status);

                var updatedInv = await verifyCtx.InventoryItems.FindAsync(invId);
                Assert.NotNull(updatedInv);
                Assert.Equal(0, updatedInv.QuantityReserved);
                Assert.Equal(20, updatedInv.Available);
            }
        }

        // =========================================================================
        // D-04: cleanup must not be wedged by a single unclosable order
        // =========================================================================
        [Fact]
        public async Task AbandonedOrderCleanupService_SkipsShippedAndDeliveredOrders_AndStillCancelsTheRest()
        {
            var dbName = Guid.NewGuid().ToString();
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var invId = Guid.NewGuid();
            var shippedId = Guid.NewGuid();
            var deliveredId = Guid.NewGuid();
            var cancellableId = Guid.NewGuid();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var invItem = new InventoryItem(productId, warehouseId, quantityOnHand: 30);
                invItem.Id = invId;
                invItem.Reserve(6); // 2 units per stale order
                await ctx.InventoryItems.AddAsync(invItem);

                await ctx.Orders.AddAsync(StaleOrder(shippedId, "ORD-SHIPPED", productId, FulfillmentStatus.Shipped));
                await ctx.Orders.AddAsync(StaleOrder(deliveredId, "ORD-DELIVERED", productId, FulfillmentStatus.Delivered));
                await ctx.Orders.AddAsync(StaleOrder(cancellableId, "ORD-OPEN", productId, FulfillmentStatus.Unfulfilled));

                await ctx.SaveChangesAsync();
            }

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            var provider = services.BuildServiceProvider();

            var cleanupService = new AbandonedOrderCleanupService(
                provider,
                NullLogger<AbandonedOrderCleanupService>.Instance,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30));

            // The shipped/delivered orders are rejected by Order.Cancel(); before the fix they
            // were pulled into the batch and their exception aborted every other order.
            var cleanedCount = await cleanupService.CleanupAbandonedOrdersAsync(CancellationToken.None);
            Assert.Equal(1, cleanedCount);

            using (var verifyCtx = CreateInMemoryContext(dbName))
            {
                Assert.Equal(OrderStatus.Cancelled, (await verifyCtx.Orders.FindAsync(cancellableId))!.Status);
                Assert.Equal(OrderStatus.Placed, (await verifyCtx.Orders.FindAsync(shippedId))!.Status);
                Assert.Equal(OrderStatus.Placed, (await verifyCtx.Orders.FindAsync(deliveredId))!.Status);

                // Only the cancelled order's 2 units were released (6 - 2 = 4).
                Assert.Equal(4, (await verifyCtx.InventoryItems.FindAsync(invId))!.QuantityReserved);
            }
        }

        [Fact]
        public async Task AbandonedOrderCleanupService_OneFailingOrder_DoesNotBlockTheBatch()
        {
            var dbName = Guid.NewGuid().ToString();
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var invId = Guid.NewGuid();
            var failingId = Guid.NewGuid();
            var goodId1 = Guid.NewGuid();
            var goodId2 = Guid.NewGuid();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var invItem = new InventoryItem(productId, warehouseId, quantityOnHand: 30);
                invItem.Id = invId;
                invItem.Reserve(6);
                await ctx.InventoryItems.AddAsync(invItem);

                await ctx.Orders.AddAsync(StaleOrder(failingId, "ORD-FAIL", productId, FulfillmentStatus.Unfulfilled));
                await ctx.Orders.AddAsync(StaleOrder(goodId1, "ORD-OK-1", productId, FulfillmentStatus.Unfulfilled));
                await ctx.Orders.AddAsync(StaleOrder(goodId2, "ORD-OK-2", productId, FulfillmentStatus.Unfulfilled));

                await ctx.SaveChangesAsync();
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var services = new ServiceCollection();
            services.AddScoped<IApplicationDbContext>(_ => new FailingSaveDbContext(options, failingId));
            var provider = services.BuildServiceProvider();

            var cleanupService = new AbandonedOrderCleanupService(
                provider,
                NullLogger<AbandonedOrderCleanupService>.Instance,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30));

            // With a single shared SaveChangesAsync, the one failing order took the whole batch
            // down with it. Per-order try/catch + per-order save isolates it.
            var cleanedCount = await cleanupService.CleanupAbandonedOrdersAsync(CancellationToken.None);
            Assert.Equal(2, cleanedCount);

            using (var verifyCtx = CreateInMemoryContext(dbName))
            {
                Assert.Equal(OrderStatus.Placed, (await verifyCtx.Orders.FindAsync(failingId))!.Status);
                Assert.Equal(OrderStatus.Cancelled, (await verifyCtx.Orders.FindAsync(goodId1))!.Status);
                Assert.Equal(OrderStatus.Cancelled, (await verifyCtx.Orders.FindAsync(goodId2))!.Status);

                // The two successful orders released 2 units each; the failing one released none.
                Assert.Equal(2, (await verifyCtx.InventoryItems.FindAsync(invId))!.QuantityReserved);
            }
        }

        private static Order StaleOrder(Guid id, string orderNumber, Guid productId, FulfillmentStatus fulfillmentStatus)
        {
            var order = new Order { Id = id, OrderNumber = orderNumber };
            order.AddItem(productId, Guid.Empty, "Test Product", 50m, 2);
            SetPrivateProperty(order, "Status", OrderStatus.Placed);
            SetPrivateProperty(order, "FulfillmentStatus", fulfillmentStatus);
            SetPrivateProperty(order, "CreatedAt", DateTimeOffset.UtcNow.AddMinutes(-40));
            return order;
        }

        /// <summary>
        /// Fails the save of one specific order so the cleanup loop's per-order isolation can be
        /// observed without depending on a domain rule.
        /// </summary>
        private sealed class FailingSaveDbContext : ApplicationDbContext
        {
            private readonly Guid _failingOrderId;

            public FailingSaveDbContext(DbContextOptions<ApplicationDbContext> options, Guid failingOrderId)
                : base(options)
            {
                _failingOrderId = failingOrderId;
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                var touchesFailingOrder = ChangeTracker.Entries<Order>()
                    .Any(e => e.Entity.Id == _failingOrderId && e.State == EntityState.Modified);

                if (touchesFailingOrder)
                {
                    throw new InvalidOperationException("Simulated persistence failure.");
                }

                return base.SaveChangesAsync(cancellationToken);
            }
        }

        // =========================================================================
        // Issue 2: User Management Pagination Fix
        // =========================================================================
        [Fact]
        public async Task UserManagementService_GetUsersAsync_ReturnsCorrectTotalCountAndPage()
        {
            using var ctx = CreateInMemoryContext();
            var userManager = CreateUserManager(ctx);
            var mapper = CreateMapper();
            var fakeRefreshTokenService = new FakeRefreshTokenService();
            var fakeCurrentUser = new FakeCurrentUserService();

            var service = new UserManagementService(userManager, mapper, fakeRefreshTokenService, fakeCurrentUser);

            for (int i = 1; i <= 7; i++)
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = $"user{i}",
                    Email = $"user{i}@example.com",
                    FirstName = $"First{i}",
                    LastName = $"Last{i}",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
                };
                await userManager.CreateAsync(user, "Password123!");
            }

            var result = await service.GetUsersAsync(page: 2, pageSize: 3, null, null, null, false);

            Assert.NotNull(result);
            Assert.Equal(7, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(3, result.PageSize);
            Assert.Equal(3, result.Items.Count);
        }

        // =========================================================================
        // Issue 3: ExportReportQueryHandler CSV and JSON Generation
        // =========================================================================
        [Fact]
        public async Task ExportReportQueryHandler_GeneratesPopulatedCsv_ForSalesReport()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();
            var userId = Guid.NewGuid();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-001",
                UserId = userId
            };
            SetPrivateProperty(order, "Status", OrderStatus.Paid);
            SetPrivateProperty(order, "TotalAmount", 150m);
            SetPrivateProperty(order, "CreatedAt", DateTimeOffset.UtcNow.AddDays(-2));
            await ctx.Orders.AddAsync(order);
            await ctx.SaveChangesAsync();

            var handler = new ExportReportQueryHandler(ctx, mapper);
            var query = new ExportReportQuery
            {
                ReportType = "sales",
                Parameters = new ReportParameters
                {
                    Format = "csv",
                    StartDate = DateTimeOffset.UtcNow.AddDays(-10),
                    EndDate = DateTimeOffset.UtcNow
                }
            };

            var result = await handler.Handle(query);

            Assert.NotNull(result);
            Assert.Equal("text/csv", result.ContentType);
            Assert.Contains("sales_report_", result.FileName);

            var csvContent = System.Text.Encoding.UTF8.GetString(result.Content);
            Assert.Contains("Period,Orders,Revenue,NewCustomers", csvContent);
            Assert.Contains("150.00", csvContent);
        }

        [Fact]
        public async Task ExportReportQueryHandler_GeneratesJsonExport_ForRevenueReport()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-REV-001"
            };
            SetPrivateProperty(order, "Status", OrderStatus.Paid);
            SetPrivateProperty(order, "TotalAmount", 200m);
            SetPrivateProperty(order, "CreatedAt", DateTimeOffset.UtcNow.AddDays(-1));
            await ctx.Orders.AddAsync(order);
            await ctx.SaveChangesAsync();

            var handler = new ExportReportQueryHandler(ctx, mapper);
            var query = new ExportReportQuery
            {
                ReportType = "revenue",
                Parameters = new ReportParameters
                {
                    Format = "json",
                    StartDate = DateTimeOffset.UtcNow.AddDays(-5),
                    EndDate = DateTimeOffset.UtcNow
                }
            };

            var result = await handler.Handle(query);

            Assert.NotNull(result);
            Assert.Equal("application/json", result.ContentType);
            var json = System.Text.Encoding.UTF8.GetString(result.Content);
            Assert.Contains("grossRevenue", json, StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================================
        // Issue 4: Admin Products Variant Stock Mapping
        // =========================================================================
        [Fact]
        public void MappingProfile_MapsVariantStockCorrectly_ToAdminProductDto()
        {
            var mapper = CreateMapper();
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "Variant-Only Product",
                Sku = "VAR-PROD",
                Slug = "variant-only-product",
                BasePrice = 100m,
                Variants = new List<ProductVariant>
                {
                    new ProductVariant
                    {
                        Id = variantId,
                        ProductId = productId,
                        Name = "Large / Red",
                        Sku = "VAR-PROD-LR",
                        Price = 120m,
                        IsActive = true,
                        InventoryItems = new List<InventoryItem>
                        {
                            new InventoryItem(productId, warehouseId, quantityOnHand: 15, productVariantId: variantId)
                        }
                    }
                }
            };

            var dto = mapper.Map<AdminProductDto>(product);

            Assert.NotNull(dto);
            Assert.Equal(15, dto.Stock);
            Assert.Equal(15, dto.AvailableStock);
        }

        [Fact]
        public async Task GetAdminProductsQueryHandler_IncludesVariantInventory()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();

            var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Main Warehouse", Code = "MWH-1" };
            await ctx.Warehouses.AddAsync(warehouse);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "T-Shirt",
                Sku = "TSHIRT-001",
                Slug = "t-shirt",
                BasePrice = 25m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Products.AddAsync(product);

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = "XL Black",
                Sku = "TSHIRT-XL-BLK",
                Price = 28m,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.ProductVariants.AddAsync(variant);

            var inv = new InventoryItem(product.Id, warehouse.Id, quantityOnHand: 30, productVariantId: variant.Id);
            await ctx.InventoryItems.AddAsync(inv);

            await ctx.SaveChangesAsync();

            var handler = new GetAdminProductsQueryHandler(ctx, mapper);
            var result = await handler.Handle(new GetAdminProductsQuery { Page = 1, PageSize = 10 });

            Assert.NotNull(result);
            Assert.Single(result.Items);
            var item = result.Items.First();
            Assert.Equal(30, item.Stock);
            Assert.Equal(30, item.AvailableStock);
        }

        // =========================================================================
        // Issue 5: Self-Demotion and Self-Deactivation Safeguards
        // =========================================================================
        [Fact]
        public async Task UserManagementService_PreventsSelfDeactivation()
        {
            using var ctx = CreateInMemoryContext();
            var userManager = CreateUserManager(ctx);
            var mapper = CreateMapper();
            var fakeRefreshTokenService = new FakeRefreshTokenService();
            var adminId = Guid.NewGuid();

            var fakeCurrentUser = new FakeCurrentUserService(adminId, isAdmin: true);

            var adminUser = new ApplicationUser
            {
                Id = adminId,
                UserName = "rootadmin",
                Email = "admin@store.com",
                IsActive = true
            };
            await userManager.CreateAsync(adminUser, "Password123!");

            var service = new UserManagementService(userManager, mapper, fakeRefreshTokenService, fakeCurrentUser);

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                service.UpdateUserAsync(adminId, "admin@store.com", "rootadmin", "Admin", "User", "Admin", "12345", isActive: false, isEmailVerified: true, isPhoneVerified: true, roles: new List<string> { "Admin" }));

            Assert.Equal("Cannot deactivate your own account.", ex.Message);
        }

        [Fact]
        public async Task UserManagementService_PreventsSelfDemotion()
        {
            using var ctx = CreateInMemoryContext();
            var userManager = CreateUserManager(ctx);
            var mapper = CreateMapper();
            var fakeRefreshTokenService = new FakeRefreshTokenService();
            var adminId = Guid.NewGuid();

            var fakeCurrentUser = new FakeCurrentUserService(adminId, isAdmin: true);

            var adminUser = new ApplicationUser
            {
                Id = adminId,
                UserName = "rootadmin",
                Email = "admin@store.com",
                IsActive = true
            };
            await userManager.CreateAsync(adminUser, "Password123!");

            var service = new UserManagementService(userManager, mapper, fakeRefreshTokenService, fakeCurrentUser);

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                service.SetUserRolesAsync(adminId, new List<string> { "Customer" }));

            Assert.Equal("Cannot remove the Admin role from your own account.", ex.Message);
        }

        [Fact]
        public async Task UserManagementService_PreventsSelfDeletion()
        {
            using var ctx = CreateInMemoryContext();
            var userManager = CreateUserManager(ctx);
            var mapper = CreateMapper();
            var fakeRefreshTokenService = new FakeRefreshTokenService();
            var adminId = Guid.NewGuid();

            var fakeCurrentUser = new FakeCurrentUserService(adminId, isAdmin: true);

            var adminUser = new ApplicationUser
            {
                Id = adminId,
                UserName = "rootadmin",
                Email = "admin@store.com",
                IsActive = true
            };
            await userManager.CreateAsync(adminUser, "Password123!");

            var service = new UserManagementService(userManager, mapper, fakeRefreshTokenService, fakeCurrentUser);

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                service.DeleteUserAsync(adminId, hardDelete: true));

            Assert.Equal("Cannot delete your own account.", ex.Message);
        }
    }
}
