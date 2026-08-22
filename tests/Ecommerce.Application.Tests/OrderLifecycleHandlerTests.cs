using System;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Orders;
using Ecommerce.Application.Mappings;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class OrderLifecycleHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
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

        private static async Task<Order> SeedPlacedOrderAsync(ApplicationDbContext context)
        {
            var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-TEST" };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 10m, 1);
            order.PlaceOrder();
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            return order;
        }

        [Fact]
        public async Task MarkPaid_TransitionsOrderToPaid()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();
            var order = await SeedPlacedOrderAsync(context);

            var handler = new MarkOrderPaidCommandHandler(context, mapper, currentUser);
            var result = await handler.Handle(new MarkOrderPaidCommand { OrderId = order.Id });

            Assert.Equal("Paid", result.Status);
            Assert.Equal("Paid", result.PaymentStatus);
        }

        [Fact]
        public async Task MarkPaid_UnknownOrder_ThrowsNotFound()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();
            var handler = new MarkOrderPaidCommandHandler(context, mapper, currentUser);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new MarkOrderPaidCommand { OrderId = Guid.NewGuid() }));
        }

        [Fact]
        public async Task Complete_FromPaid_TransitionsToCompleted()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();
            var order = await SeedPlacedOrderAsync(context);

            await new MarkOrderPaidCommandHandler(context, mapper, currentUser).Handle(new MarkOrderPaidCommand { OrderId = order.Id });

            var result = await new CompleteOrderCommandHandler(context, mapper, currentUser)
                .Handle(new CompleteOrderCommand { OrderId = order.Id });

            Assert.Equal("Completed", result.Status);
        }

        [Fact]
        public async Task Complete_FromUnpaid_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();
            var order = await SeedPlacedOrderAsync(context);

            await Assert.ThrowsAsync<DomainException>(() =>
                new CompleteOrderCommandHandler(context, mapper, currentUser).Handle(new CompleteOrderCommand { OrderId = order.Id }));
        }

        [Fact]
        public async Task Cancel_FromPaid_SetsCancelled()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();
            var order = await SeedPlacedOrderAsync(context);

            await new MarkOrderPaidCommandHandler(context, mapper, currentUser).Handle(new MarkOrderPaidCommand { OrderId = order.Id });

            var result = await new CancelOrderCommandHandler(context, mapper, currentUser)
                .Handle(new CancelOrderCommand { OrderId = order.Id, Reason = "changed mind" });

            Assert.Equal("Cancelled", result.Status);
        }

        [Fact]
        public async Task Cancel_FromCompleted_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currentUser = new TestCurrentUserService();
            var order = await SeedPlacedOrderAsync(context);

            await new MarkOrderPaidCommandHandler(context, mapper, currentUser).Handle(new MarkOrderPaidCommand { OrderId = order.Id });
            await new CompleteOrderCommandHandler(context, mapper, currentUser).Handle(new CompleteOrderCommand { OrderId = order.Id });

            await Assert.ThrowsAsync<DomainException>(() =>
                new CancelOrderCommandHandler(context, mapper, currentUser).Handle(new CancelOrderCommand { OrderId = order.Id }));
        }

        [Fact]
        public async Task GetAdminOrders_PopulatesCustomerDetailsAndAddress()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var user = new Ecommerce.Infrastructure.Identity.ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = "Mohammad",
                LastName = "Sofan",
                Email = "mohammad.n.sofan@gmail.com",
                PhoneNumber = "+970599123456"
            };
            context.Set<Ecommerce.Infrastructure.Identity.ApplicationUser>().Add(user);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-1",
                UserId = user.Id,
                Notes = "Address: Ramallah, Main St | PaymentMethod: CashOnDelivery"
            };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Laptop", 1000m, 1);
            order.PlaceOrder();
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var handler = new Queries.Admin.GetAdminOrdersQueryHandler(context, mapper);
            var result = await handler.Handle(new Queries.Admin.GetAdminOrdersQuery());

            Assert.NotNull(result);
            Assert.Single(result.Items);
            var item = result.Items[0];
            Assert.Equal("Mohammad Sofan", item.CustomerName);
            Assert.Equal("mohammad.n.sofan@gmail.com", item.CustomerEmail);
            Assert.Equal("+970599123456", item.CustomerPhone);
            Assert.Equal("Ramallah, Main St", item.ShippingAddress);
            Assert.Equal("CashOnDelivery", item.PaymentMethod);
        }

        [Fact]
        public async Task GetAdminOrders_SearchByCustomerEmail_ReturnsMatchingOrder()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var user1 = new Ecommerce.Infrastructure.Identity.ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = "Mohammad",
                LastName = "Sofan",
                Email = "mohammad.n.sofan@gmail.com",
                PhoneNumber = "+970599123456"
            };
            var user2 = new Ecommerce.Infrastructure.Identity.ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = "Ahmad",
                LastName = "Ali",
                Email = "ahmad@example.com",
                PhoneNumber = "+970599654321"
            };
            context.Set<Ecommerce.Infrastructure.Identity.ApplicationUser>().AddRange(user1, user2);

            var order1 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-1", UserId = user1.Id };
            order1.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P1", 100m, 1);
            order1.PlaceOrder();

            var order2 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2", UserId = user2.Id };
            order2.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P2", 200m, 1);
            order2.PlaceOrder();

            context.Orders.AddRange(order1, order2);
            await context.SaveChangesAsync();

            var handler = new Queries.Admin.GetAdminOrdersQueryHandler(context, mapper);
            var result = await handler.Handle(new Queries.Admin.GetAdminOrdersQuery { Search = "sofan" });

            Assert.Single(result.Items);
            Assert.Equal("ORD-1", result.Items[0].OrderNumber);
            Assert.Equal("Mohammad Sofan", result.Items[0].CustomerName);
        }

        [Fact]
        public async Task GetAdminOrderById_PopulatesCustomerDetails()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var user = new Ecommerce.Infrastructure.Identity.ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                PhoneNumber = "+970599000111"
            };
            context.Set<Ecommerce.Infrastructure.Identity.ApplicationUser>().Add(user);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-DETAIL",
                UserId = user.Id,
                Notes = "Address: Nablus, Rafidia | PaymentMethod: Stripe"
            };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Shoes", 50m, 2);
            order.PlaceOrder();
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var handler = new Queries.Admin.GetAdminOrderByIdQueryHandler(context, mapper);
            var result = await handler.Handle(new Queries.Admin.GetAdminOrderByIdQuery { Id = order.Id });

            Assert.NotNull(result);
            Assert.Equal("Jane Doe", result.CustomerName);
            Assert.Equal("jane@example.com", result.CustomerEmail);
            Assert.Equal("+970599000111", result.CustomerPhone);
            Assert.Equal("Nablus, Rafidia", result.ShippingAddress);
            Assert.Equal("Stripe", result.PaymentMethod);
        }
    }
}

