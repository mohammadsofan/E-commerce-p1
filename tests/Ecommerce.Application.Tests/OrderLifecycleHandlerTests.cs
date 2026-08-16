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
            var order = await SeedPlacedOrderAsync(context);

            var handler = new MarkOrderPaidCommandHandler(context, mapper);
            var result = await handler.Handle(new MarkOrderPaidCommand { OrderId = order.Id });

            Assert.Equal("Paid", result.Status);
            Assert.Equal("Paid", result.PaymentStatus);
        }

        [Fact]
        public async Task MarkPaid_UnknownOrder_ThrowsNotFound()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var handler = new MarkOrderPaidCommandHandler(context, mapper);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new MarkOrderPaidCommand { OrderId = Guid.NewGuid() }));
        }

        [Fact]
        public async Task Complete_FromPaid_TransitionsToCompleted()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var order = await SeedPlacedOrderAsync(context);

            await new MarkOrderPaidCommandHandler(context, mapper).Handle(new MarkOrderPaidCommand { OrderId = order.Id });

            var result = await new CompleteOrderCommandHandler(context, mapper)
                .Handle(new CompleteOrderCommand { OrderId = order.Id });

            Assert.Equal("Completed", result.Status);
        }

        [Fact]
        public async Task Complete_FromUnpaid_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var order = await SeedPlacedOrderAsync(context);

            await Assert.ThrowsAsync<DomainException>(() =>
                new CompleteOrderCommandHandler(context, mapper).Handle(new CompleteOrderCommand { OrderId = order.Id }));
        }

        [Fact]
        public async Task Cancel_FromPaid_SetsCancelled()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var order = await SeedPlacedOrderAsync(context);

            await new MarkOrderPaidCommandHandler(context, mapper).Handle(new MarkOrderPaidCommand { OrderId = order.Id });

            var result = await new CancelOrderCommandHandler(context, mapper)
                .Handle(new CancelOrderCommand { OrderId = order.Id, Reason = "changed mind" });

            Assert.Equal("Cancelled", result.Status);
        }

        [Fact]
        public async Task Cancel_FromCompleted_ThrowsDomainException()
        {
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();
            var order = await SeedPlacedOrderAsync(context);

            await new MarkOrderPaidCommandHandler(context, mapper).Handle(new MarkOrderPaidCommand { OrderId = order.Id });
            await new CompleteOrderCommandHandler(context, mapper).Handle(new CompleteOrderCommand { OrderId = order.Id });

            await Assert.ThrowsAsync<DomainException>(() =>
                new CancelOrderCommandHandler(context, mapper).Handle(new CancelOrderCommand { OrderId = order.Id }));
        }
    }
}
