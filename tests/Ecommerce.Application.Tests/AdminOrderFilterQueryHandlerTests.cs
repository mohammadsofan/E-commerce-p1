using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Mappings;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminOrderFilterQueryHandlerTests
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
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
                cfg.AddProfile<AutoMapperProfileForTests>();
            });
            return config.CreateMapper();
        }

        [Fact]
        public async Task Handle_FiltersByStatus_ReturnsOnlyMatchingOrders()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            // Order 1: Placed
            var order1 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001" };
            order1.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Item 1", 50m, 1);
            order1.PlaceOrder();
            await context.Orders.AddAsync(order1);

            // Order 2: Completed
            var order2 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-002" };
            order2.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Item 2", 100m, 1);
            order2.PlaceOrder();
            order2.MarkPaid();
            order2.Complete();
            await context.Orders.AddAsync(order2);

            await context.SaveChangesAsync();

            var handler = new GetAdminOrdersQueryHandler(context, mapper);

            // Act - Filter by ""Completed"" string
            var resultString = await handler.Handle(new GetAdminOrdersQuery { Status = "Completed" });

            // Act - Filter by OrderStatus.Placed enum
            var resultEnum = await handler.Handle(new GetAdminOrdersQuery { OrderStatus = OrderStatus.Placed });

            // Assert
            Assert.Single(resultString.Items);
            Assert.Equal("ORD-002", resultString.Items[0].OrderNumber);
            Assert.Equal("Completed", resultString.Items[0].Status);

            Assert.Single(resultEnum.Items);
            Assert.Equal("ORD-001", resultEnum.Items[0].OrderNumber);
            Assert.Equal("Placed", resultEnum.Items[0].Status);
        }

        [Fact]
        public async Task Handle_FiltersByDateRange_ReturnsOrdersWithinRange()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mapper = CreateMapper();

            var now = DateTimeOffset.UtcNow;

            // Order 1: Placed
            var order1 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-OLD" };
            order1.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Item Old", 30m, 1);
            order1.PlaceOrder();
            await context.Orders.AddAsync(order1);

            // Order 2: Placed today
            var order2 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-RECENT" };
            order2.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Item Recent", 60m, 1);
            order2.PlaceOrder();
            await context.Orders.AddAsync(order2);

            await context.SaveChangesAsync();

            // Adjust CreatedAt for test isolation
            var entry1 = context.Entry(order1);
            entry1.Property("CreatedAt").CurrentValue = now.AddDays(-10);
            var entry2 = context.Entry(order2);
            entry2.Property("CreatedAt").CurrentValue = now;
            await context.SaveChangesAsync();

            var handler = new GetAdminOrdersQueryHandler(context, mapper);

            // Act: Filter orders created in the last 2 days
            var result = await handler.Handle(new GetAdminOrdersQuery
            {
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(1)
            });

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("ORD-RECENT", result.Items[0].OrderNumber);
        }
    }
}
