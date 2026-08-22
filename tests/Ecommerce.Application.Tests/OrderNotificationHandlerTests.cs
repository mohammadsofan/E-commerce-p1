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

        private static ApplicationUser CreateUser(Guid id, string email = "customer@test.com", string? phone = null, bool includePhone = true)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                PhoneNumber = includePhone ? (phone ?? "+15551234567") : string.Empty,
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

            public Task SendOrderConfirmationAsync(Order order, string customerEmail, CancellationToken cancellationToken = default)
            {
                Sent.Add(new EmailMessage { To = customerEmail, Subject = $"Order {order.OrderNumber} confirmed", Body = "Confirmation" });
                return Task.CompletedTask;
            }

            public Task SendAdminOrderAlertAsync(Order order, CancellationToken cancellationToken = default)
            {
                Sent.Add(new EmailMessage { To = "admin@example.com", Subject = $"Admin alert for {order.OrderNumber}", Body = "Alert" });
                return Task.CompletedTask;
            }

            public Task SendOrderShippedAsync(Order order, string customerEmail, CancellationToken cancellationToken = default)
            {
                Sent.Add(new EmailMessage { To = customerEmail, Subject = $"Order {order.OrderNumber} shipped", Body = "Shipped" });
                return Task.CompletedTask;
            }
        }

        private class FakeSmsService : ISmsService
        {
            public List<SmsMessage> Sent { get; } = new List<SmsMessage>();

            public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }
        }

        private class FakePushService : IPushNotificationService
        {
            public List<PushMessage> Sent { get; } = new List<PushMessage>();

            public Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }
        }

        private static OrderPlacedEventHandler CreateHandler(
            ApplicationDbContext ctx,
            IEmailService email,
            ISmsService? sms = null,
            IPushNotificationService? push = null)
        {
            return new OrderPlacedEventHandler(
                ctx,
                email,
                sms ?? new FakeSmsService(),
                push ?? new FakePushService(),
                NullLogger<OrderPlacedEventHandler>.Instance);
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
            var handler = CreateHandler(ctx, emailService);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));
            await Task.Delay(100);

            var message = Assert.Single(emailService.Sent);
            Assert.Equal("customer@test.com", message.To);
            Assert.Contains(order.OrderNumber, message.Subject);
            Assert.True(message.IsHtml);
            Assert.Contains("Test Product", message.Body);
            Assert.Contains(order.TotalAmount.ToString("C"), message.Body);

            var notification = await ctx.Notifications.SingleAsync(n => n.Channel == "email");
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
            var handler = CreateHandler(ctx, emailService);

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
            var handler = CreateHandler(ctx, emailService);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            Assert.Empty(emailService.Sent);
            Assert.DoesNotContain(ctx.Notifications, n => n.Channel == "email");
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
            var handler = CreateHandler(ctx, emailService);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));
            await Task.Delay(100);

            var notification = await ctx.Notifications.SingleOrDefaultAsync(n => n.Channel == "email");
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

            public Task SendOrderConfirmationAsync(Order order, string customerEmail, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("SMTP boom");
            }

            public Task SendAdminOrderAlertAsync(Order order, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("SMTP boom");
            }

            public Task SendOrderShippedAsync(Order order, string customerEmail, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("SMTP boom");
            }
        }

        [Fact]
        public async Task Handle_SmsEnabled_SendsSmsAndPersistsNotification()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var sms = new FakeSmsService();
            var handler = CreateHandler(ctx, new FakeEmailService(), sms: sms);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            var message = Assert.Single(sms.Sent);
            Assert.Equal("+15551234567", message.To);
            Assert.Contains(order.OrderNumber, message.Body);

            var notification = await ctx.Notifications.SingleAsync(n => n.Channel == "sms");
            Assert.Equal("OrderPlaced", notification.Type);
            Assert.Equal("sms", notification.Channel);
            Assert.Equal("sent", notification.Status);
        }

        [Fact]
        public async Task Handle_NoPhoneNumber_SkipsSms()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId, includePhone: false));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var sms = new FakeSmsService();
            var handler = CreateHandler(ctx, new FakeEmailService(), sms: sms);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            Assert.Empty(sms.Sent);
            Assert.DoesNotContain(ctx.Notifications, n => n.Channel == "sms");
        }

        [Fact]
        public async Task Handle_UserDisabledSmsPreference_SkipsSms()
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
                Channel = "sms",
                IsEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            var sms = new FakeSmsService();
            var handler = CreateHandler(ctx, new FakeEmailService(), sms: sms);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            Assert.Empty(sms.Sent);
            Assert.DoesNotContain(ctx.Notifications, n => n.Channel == "sms");
        }

        [Fact]
        public async Task Handle_SmsProviderThrows_RecordsFailedNotification()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var sms = new ThrowingSmsService();
            var handler = CreateHandler(ctx, new FakeEmailService(), sms: sms);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            var notification = await ctx.Notifications.SingleOrDefaultAsync(n => n.Channel == "sms");
            Assert.NotNull(notification);
            Assert.Equal("failed", notification.Status);
            Assert.Contains("sms boom", notification.ErrorMessage);
        }

        [Fact]
        public async Task Handle_PushChannelConfigured_SendsPushAndPersistsNotification()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            ctx.NotificationChannels.Add(new NotificationChannel
            {
                Id = Guid.NewGuid(),
                Name = "push",
                Provider = "fcm",
                ConfigurationJson = "{}",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            var push = new FakePushService();
            var handler = CreateHandler(ctx, new FakeEmailService(), push: push);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            var message = Assert.Single(push.Sent);
            Assert.Contains(order.OrderNumber, message.Body);

            var notification = await ctx.Notifications.SingleAsync(n => n.Channel == "push");
            Assert.Equal("OrderPlaced", notification.Type);
            Assert.Equal("push", notification.Channel);
            Assert.Equal("sent", notification.Status);
        }

        [Fact]
        public async Task Handle_NoPushChannelConfigured_SkipsPush()
        {
            using var ctx = CreateContext();

            var userId = Guid.NewGuid();
            ctx.Set<ApplicationUser>().Add(CreateUser(userId));
            var order = CreateOrder(userId);
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var push = new FakePushService();
            var handler = CreateHandler(ctx, new FakeEmailService(), push: push);

            await handler.Handle(new OrderPlacedDomainEvent(order.Id));

            Assert.Empty(push.Sent);
            Assert.DoesNotContain(ctx.Notifications, n => n.Channel == "push");
        }

        private class ThrowingSmsService : ISmsService
        {
            public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("sms boom");
            }
        }
    }
}

