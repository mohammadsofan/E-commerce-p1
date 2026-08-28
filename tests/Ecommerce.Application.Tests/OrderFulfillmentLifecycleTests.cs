using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Commands.Orders;
using Ecommerce.Application.Mappings;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    /// <summary>
    /// Covers the fulfilment lifecycle gaps found by the QA audit: reservations leaking when an
    /// order is completed (D-03), refunds on never-paid orders (D-09), and /ship not producing a
    /// trackable Shipment (D-10).
    /// </summary>
    public class OrderFulfillmentLifecycleTests
    {
        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private class TestCurrentUserService : Ecommerce.Application.Interfaces.ICurrentUserService
        {
            public Guid? UserId { get; set; }
            public string? UserName { get; set; } = "TestUser";
            public bool IsAdmin { get; set; } = true;
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            return config.CreateMapper();
        }

        /// <summary>
        /// Seeds a paid order for a single product whose stock is already reserved, mirroring the
        /// state left behind by checkout.
        /// </summary>
        private static async Task<(Order order, InventoryItem inventory, Warehouse warehouse)> SeedReservedPaidOrderAsync(
            ApplicationDbContext context,
            int onHand = 20,
            int quantity = 4,
            string paymentMethod = "CashOnDelivery")
        {
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = "Main",
                Code = "WH-MAIN",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.Warehouses.Add(warehouse);

            var inventory = new InventoryItem(productId, warehouse.Id, onHand, variantId);
            inventory.Reserve(quantity);
            context.InventoryItems.Add(inventory);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-FULFIL-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                PaymentMethod = paymentMethod
            };
            order.AddItem(productId, variantId, "Product", 10m, quantity);
            order.PlaceOrder();
            order.MarkPaid();

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            return (order, inventory, warehouse);
        }

        // ---------- D-03: completing an order must not leak the reservation ----------

        [Fact]
        public async Task Complete_ConsumesRemainingReservation_SoStockIsNotLeftLocked()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();

            var (order, inventory, _) = await SeedReservedPaidOrderAsync(context, onHand: 20, quantity: 4);

            Assert.Equal(4, inventory.QuantityReserved);
            Assert.Equal(20, inventory.QuantityOnHand);

            var result = await new CompleteOrderCommandHandler(context, mapper, currentUser)
                .Handle(new CompleteOrderCommand { OrderId = order.Id });

            Assert.Equal("Completed", result.Status);

            var updated = await context.InventoryItems.FindAsync(inventory.Id);
            Assert.NotNull(updated);
            Assert.Equal(0, updated!.QuantityReserved);
            Assert.Equal(16, updated.QuantityOnHand);
            Assert.Equal(16, updated.Available);
        }

        [Fact]
        public async Task Complete_AfterDelivery_DoesNotDoubleDeductStock()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();

            var (order, inventory, _) = await SeedReservedPaidOrderAsync(context, onHand: 20, quantity: 4);

            await new MarkOrderShippedCommandHandler(context)
                .Handle(new MarkOrderShippedCommand { OrderId = order.Id, TrackingNumber = "T-1", Carrier = "QA" });
            await new MarkOrderDeliveredCommandHandler(context)
                .Handle(new MarkOrderDeliveredCommand { OrderId = order.Id });

            var afterDelivery = await context.InventoryItems.FindAsync(inventory.Id);
            Assert.Equal(16, afterDelivery!.QuantityOnHand);
            Assert.Equal(0, afterDelivery.QuantityReserved);

            await new CompleteOrderCommandHandler(context, mapper, currentUser)
                .Handle(new CompleteOrderCommand { OrderId = order.Id });

            var afterComplete = await context.InventoryItems.FindAsync(inventory.Id);
            Assert.Equal(16, afterComplete!.QuantityOnHand);
            Assert.Equal(0, afterComplete.QuantityReserved);
        }

        // ---------- D-09: refunding a never-paid order must be rejected ----------

        [Fact]
        public async Task Refund_NeverPaidOrder_Throws()
        {
            using var context = CreateInMemoryContext();

            var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-NOPAY", CurrencyCode = "ILS" };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 100m, 1);
            order.PlaceOrder();
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var handler = new ProcessOrderRefundCommandHandler(context);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new ProcessOrderRefundCommand { OrderId = order.Id, Amount = 100m, Reason = "should be rejected" }));

            var reloaded = await context.Orders.FindAsync(order.Id);
            Assert.Equal(0m, reloaded!.RefundedAmount);
            Assert.Equal(PaymentStatus.Pending, reloaded.PaymentStatus);
            Assert.Equal(OrderStatus.Placed, reloaded.Status);
        }

        [Fact]
        public async Task Refund_PaidOrder_Succeeds()
        {
            using var context = CreateInMemoryContext();

            var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-PAID", CurrencyCode = "ILS" };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 100m, 1);
            order.PlaceOrder();
            order.MarkPaid();
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            await new ProcessOrderRefundCommandHandler(context)
                .Handle(new ProcessOrderRefundCommand { OrderId = order.Id, Amount = 40m, Reason = "partial" });

            var reloaded = await context.Orders.FindAsync(order.Id);
            Assert.Equal(40m, reloaded!.RefundedAmount);
            Assert.Equal(PaymentStatus.PartiallyRefunded, reloaded.PaymentStatus);
        }

        [Fact]
        public async Task Return_NeverPaidOrder_Throws()
        {
            using var context = CreateInMemoryContext();

            var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-NOPAY-RET", CurrencyCode = "ILS" };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 100m, 1);
            order.PlaceOrder();
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var itemId = order.Items.First().Id;

            await Assert.ThrowsAsync<DomainException>(() =>
                new ProcessOrderReturnCommandHandler(context).Handle(new ProcessOrderReturnCommand
                {
                    OrderId = order.Id,
                    OrderItemIds = new System.Collections.Generic.List<Guid> { itemId },
                    Reason = "never paid"
                }));

            var reloaded = await context.Orders.FindAsync(order.Id);
            Assert.Equal(0m, reloaded!.RefundedAmount);
        }

        // ---------- D-10: /ship must create a trackable Shipment ----------

        [Fact]
        public async Task Ship_CreatesAndPersistsShipmentForOrder()
        {
            using var context = CreateInMemoryContext();

            var (order, inventory, warehouse) = await SeedReservedPaidOrderAsync(context, onHand: 20, quantity: 4);

            await new MarkOrderShippedCommandHandler(context)
                .Handle(new MarkOrderShippedCommand { OrderId = order.Id, TrackingNumber = "TRK-123", Carrier = "Aramex" });

            var shipment = await context.Shipments
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.OrderId == order.Id);

            Assert.NotNull(shipment);
            Assert.Equal("TRK-123", shipment!.TrackingNumber);
            Assert.Equal("Aramex", shipment.Carrier);
            Assert.Equal("Shipped", shipment.Status);
            Assert.NotNull(shipment.ShippedAt);
            Assert.Equal(warehouse.Id, shipment.WarehouseId);

            var line = Assert.Single(shipment.Items);
            Assert.Equal(order.Items.First().Id, line.OrderItemId);
            Assert.Equal(inventory.Id, line.InventoryItemId);
            Assert.Equal(4, line.Quantity);

            var reloaded = await context.Orders.FindAsync(order.Id);
            Assert.Equal(FulfillmentStatus.Shipped, reloaded!.FulfillmentStatus);
        }

        [Fact]
        public async Task Deliver_MarksShipmentDelivered()
        {
            using var context = CreateInMemoryContext();

            var (order, _, _) = await SeedReservedPaidOrderAsync(context, onHand: 20, quantity: 4);

            await new MarkOrderShippedCommandHandler(context)
                .Handle(new MarkOrderShippedCommand { OrderId = order.Id, TrackingNumber = "TRK-1", Carrier = "QA" });
            await new MarkOrderDeliveredCommandHandler(context)
                .Handle(new MarkOrderDeliveredCommand { OrderId = order.Id });

            var shipment = await context.Shipments.FirstAsync(s => s.OrderId == order.Id);
            Assert.Equal("Delivered", shipment.Status);
            Assert.NotNull(shipment.DeliveredAt);
        }

        [Fact]
        public async Task GetOrderShipment_AfterShipping_ReturnsShipment()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var (order, _, warehouse) = await SeedReservedPaidOrderAsync(context, onHand: 20, quantity: 4);

            await new MarkOrderShippedCommandHandler(context)
                .Handle(new MarkOrderShippedCommand { OrderId = order.Id, TrackingNumber = "TRK-9", Carrier = "QA" });

            var dto = await new GetOrderShipmentQueryHandler(context, mapper)
                .Handle(new GetOrderShipmentQuery { OrderId = order.Id });

            Assert.Equal(order.Id, dto.OrderId);
            Assert.Equal("TRK-9", dto.TrackingNumber);
            Assert.Equal(warehouse.Name, dto.WarehouseName);
            Assert.Single(dto.Items);
        }

        [Fact]
        public async Task GetOrderShipment_WhenAbsent_ThrowsNotFound_So404IsReturned()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var handler = new GetOrderShipmentQueryHandler(context, mapper);

            // NotFoundException maps to HTTP 404 in the API exception middleware; a plain
            // DomainException would surface as a misleading 400.
            var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new GetOrderShipmentQuery { OrderId = Guid.NewGuid() }));

            Assert.IsAssignableFrom<DomainException>(ex);
        }

        [Fact]
        public async Task GetAdminShipmentById_WhenAbsent_ThrowsNotFound()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                new GetAdminShipmentByIdQueryHandler(context, mapper)
                    .Handle(new GetAdminShipmentByIdQuery { Id = Guid.NewGuid() }));
        }
    }
}
