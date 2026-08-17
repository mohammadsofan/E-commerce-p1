using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.DomainEvents;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class OrderNotificationHandlerTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Order CreateOrder(Guid userId)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-0001",
                UserId = userId,
                CurrencyCode = "USD"
            };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Test Product", 25.00m, 2);
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Another Product", 10.00m, 1);
            order.PlaceOrder();
            return order;
        }

        private static ApplicationUser CreateUser(Guid id, string email = "customer@test.com")
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                DisplayName = "Test Customer",
                FirstName = "Test",
                LastName = "Customer",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private class FakeEmailService : IEmailService
        {
            public List<EmailMessage> Sent { get; } = new List<EmailMessage>();

            public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }

            public Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Handle_PlacedOrder_SendsEmailAndPersistsNotification()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var emailService = new FakeEmailService();
            var handler = new OrderPlacedEventHandler(ctx, emailService, NullLogger<OrderPlacedEventHandler>.Instance);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            var message = Assert.Single(emailService.Sent);
            Assert.Equal("customer@test.com", message.To);
            Assert.Contains(order.OrderNumber, message.Subject);
            Assert.True(message.IsHtml);
            Assert.Contains("Test Product", message.Body);
            Assert.Contains(order.TotalAmount.ToString("C"), message.Body);

            var notification = await ctx.Notifications.SingleAsync();
            Assert.NotNull(notification);
            Assert.Equal("OrderPlaced", notification.Type);
            Assert.Equal("email", notification.Channel);
            Assert.Equal("sent", notification.Status);
            Assert.Equal(userId, notification.UserId);
        }

        [Fact]
        public async Task Handle_NoCustomerEmail_DoesNotSendButStillLogsNotification()
        {
            using var ctx = CreateContext();

            // Order without a user (anonymous checkout) -> no email address available.
            var order = CreateOrder(Guid.Empty);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var emailService = new FakeEmailService();
            var handler = new OrderPlacedEventHandler(ctx, emailService, NullLogger<OrderPlacedEventHandler>.Instance);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            Assert.Empty(emailService.Sent);
            Assert.Empty(ctx.Notifications);
        }

        [Fact]
        public async Task Handle_UserDisabledEmailPreference_SkipsEmail()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            ctx.NotificationPreferences.Add(new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationType = "OrderPlaced",
                Channel = "email",
                IsEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            var emailService = new FakeEmailService();
            var handler = new OrderPlacedEventHandler(ctx, emailService, NullLogger<OrderPlacedEventHandler>.Instance);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            Assert.Empty(emailService.Sent);
            Assert.Empty(ctx.Notifications);
        }

        [Fact]
        public async Task Handle_EmailSmtpFails_RecordsFailedNotification()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var emailService = new ThrowingEmailService();
            var handler = new OrderPlacedEventHandler(ctx, emailService, NullLogger<OrderPlacedEventHandler>.Instance);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            var notification = await ctx.Notifications.SingleOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal("failed", notification.Status);
            Assert.Contains("boom", notification.ErrorMessage);
        }

        private class ThrowingEmailService : IEmailService
        {
            public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("SMTP boom");
            }

            public Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("SMTP boom");
            }
        }
    }
}
