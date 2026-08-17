using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminNotificationHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task NotificationTemplate_CanBeCreated()
        {
            using var ctx = CreateInMemoryContext();

            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Order Confirmation",
                Channel = "email",
                SubjectTemplate = "Order {{orderNumber}} confirmed",
                BodyTemplate = "Dear {{customerName}}, your order {{orderNumber}} has been confirmed.",
                VariablesJson = "{\"orderNumber\": \"string\", \"customerName\": \"string\"}",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.NotificationTemplates.AddAsync(template);
            await ctx.SaveChangesAsync();

            var templates = await ctx.NotificationTemplates.ToListAsync();
            Assert.Single(templates);
            Assert.Equal("Order Confirmation", templates[0].Name);
            Assert.Equal("email", templates[0].Channel);
        }

        [Fact]
        public async Task NotificationPreference_CanBeCreated()
        {
            using var ctx = CreateInMemoryContext();

            var userId = Guid.NewGuid();
            var pref = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationType = "order_confirmed",
                Channel = "email",
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.NotificationPreferences.AddAsync(pref);
            await ctx.SaveChangesAsync();

            var prefs = await ctx.NotificationPreferences.Where(p => p.UserId == userId).ToListAsync();
            Assert.Single(prefs);
            Assert.True(prefs[0].IsEnabled);
        }

        [Fact]
        public async Task NotificationChannel_CanBeCreated()
        {
            using var ctx = CreateInMemoryContext();

            var channel = new NotificationChannel
            {
                Id = Guid.NewGuid(),
                Name = "email",
                Provider = "sendgrid",
                ConfigurationJson = "{\"apiKey\": \"test\", \"fromEmail\": \"test@example.com\"}",
                IsActive = true,
                Priority = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.NotificationChannels.AddAsync(channel);
            await ctx.SaveChangesAsync();

            var channels = await ctx.NotificationChannels.Where(c => c.IsActive).ToListAsync();
            Assert.Single(channels);
            Assert.Equal("sendgrid", channels[0].Provider);
        }

        [Fact]
        public async Task Notification_CanBeCreatedWithStatusTracking()
        {
            using var ctx = CreateInMemoryContext();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Type = "order_confirmed",
                Channel = "email",
                Subject = "Order #12345 confirmed",
                Body = "Your order has been confirmed",
                DataJson = "{\"orderId\": \"12345\"}",
                Status = "pending",
                RetryCount = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Notifications.AddAsync(notification);
            await ctx.SaveChangesAsync();

            var notifications = await ctx.Notifications.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal("pending", notifications[0].Status);
            Assert.Equal(0, notifications[0].RetryCount);
        }
    }
}