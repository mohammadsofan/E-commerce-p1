using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class AdminNotificationCommandHandlerTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IMapper CreateMapper()
        {
            return new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        [Fact]
        public async Task CreateNotification_CreatesPendingNotification()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateNotificationCommandHandler(ctx, CreateMapper());

            var command = new CreateNotificationCommand
            {
                UserId = Guid.NewGuid(),
                Type = "order_confirmed",
                Channel = "email",
                Subject = "Order confirmed",
                Body = "Your order has been confirmed",
                DataJson = "{\"orderId\": \"12345\"}"
            };

            var result = await handler.Handle(command);

            Assert.NotNull(result);
            Assert.Equal("order_confirmed", result.Type);
            Assert.Equal("email", result.Channel);
            Assert.Equal("pending", result.Status);
            Assert.Equal(0, result.RetryCount);
        }

        [Fact]
        public async Task UpdateNotification_UpdatesStatus()
        {
            using var ctx = CreateInMemoryContext();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = "order_confirmed",
                Channel = "email",
                Subject = "Old",
                Body = "Old body",
                Status = "pending",
                RetryCount = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Notifications.AddAsync(notification);
            await ctx.SaveChangesAsync();

            var handler = new UpdateNotificationCommandHandler(ctx, CreateMapper());
            var command = new UpdateNotificationCommand
            {
                Id = notification.Id,
                Subject = "New",
                Body = "New body",
                Status = "sent"
            };

            var result = await handler.Handle(command);

            Assert.Equal("New", result.Subject);
            Assert.Equal("sent", result.Status);
        }

        [Fact]
        public async Task DeleteNotification_NotFound_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new DeleteNotificationCommandHandler(ctx);
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteNotificationCommand { Id = Guid.NewGuid() }));
        }

        [Fact]
        public async Task CreateNotificationTemplate_CreatesTemplate()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateNotificationTemplateCommandHandler(ctx, CreateMapper());

            var command = new CreateNotificationTemplateCommand
            {
                Name = "Order Confirmation",
                Channel = "email",
                SubjectTemplate = "Order {{orderNumber}} confirmed",
                BodyTemplate = "Dear {{customerName}}, your order has been confirmed.",
                VariablesJson = "{\"orderNumber\": \"string\"}",
                IsActive = true
            };

            var result = await handler.Handle(command);

            Assert.Equal("Order Confirmation", result.Name);
            Assert.Equal("email", result.Channel);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreateNotificationTemplate_DuplicateName_Throws()
        {
            using var ctx = CreateInMemoryContext();

            await ctx.NotificationTemplates.AddAsync(new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Dup",
                Channel = "email",
                SubjectTemplate = "S",
                BodyTemplate = "B",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            var handler = new CreateNotificationTemplateCommandHandler(ctx, CreateMapper());
            var command = new CreateNotificationTemplateCommand { Name = "Dup", Channel = "email" };
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task UpdateNotificationPreference_TogglesEnabled()
        {
            using var ctx = CreateInMemoryContext();

            var preference = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                NotificationType = "order_confirmed",
                Channel = "email",
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await ctx.NotificationPreferences.AddAsync(preference);
            await ctx.SaveChangesAsync();

            var handler = new UpdateNotificationPreferenceCommandHandler(ctx, CreateMapper());
            var command = new UpdateNotificationPreferenceCommand { Id = preference.Id, IsEnabled = false };

            var result = await handler.Handle(command);

            Assert.False(result.IsEnabled);
        }

        [Fact]
        public async Task CreateNotificationChannel_CreatesChannel()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateNotificationChannelCommandHandler(ctx, CreateMapper());

            var command = new CreateNotificationChannelCommand
            {
                Name = "email",
                Provider = "sendgrid",
                ConfigurationJson = "{\"apiKey\": \"test\"}",
                IsActive = true,
                Priority = 1
            };

            var result = await handler.Handle(command);

            Assert.Equal("email", result.Name);
            Assert.Equal("sendgrid", result.Provider);
            Assert.Equal(1, result.Priority);
        }

        [Fact]
        public async Task CreateNotificationChannel_DuplicateName_Throws()
        {
            using var ctx = CreateInMemoryContext();

            await ctx.NotificationChannels.AddAsync(new NotificationChannel
            {
                Id = Guid.NewGuid(),
                Name = "email",
                Provider = "sendgrid",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            var handler = new CreateNotificationChannelCommandHandler(ctx, CreateMapper());
            var command = new CreateNotificationChannelCommand { Name = "email", Provider = "twilio" };
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task GetNotifications_ReturnsFilteredResults()
        {
            using var ctx = CreateInMemoryContext();

            for (int i = 0; i < 2; i++)
            {
                await ctx.Notifications.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    Type = "order_confirmed",
                    Channel = "email",
                    Subject = $"Subject {i}",
                    Body = "Body",
                    Status = "pending",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            await ctx.SaveChangesAsync();

            var handler = new GetAdminNotificationsQueryHandler(ctx, CreateMapper());
            var query = new GetAdminNotificationsQuery { Page = 1, PageSize = 10, Channel = "email" };

            var result = await handler.Handle(query);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task GetNotificationTemplates_ReturnsPagedResults()
        {
            using var ctx = CreateInMemoryContext();

            await ctx.NotificationTemplates.AddAsync(new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Welcome",
                Channel = "email",
                SubjectTemplate = "Welcome",
                BodyTemplate = "Hi",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await ctx.SaveChangesAsync();

            var handler = new GetAdminNotificationTemplatesQueryHandler(ctx, CreateMapper());
            var query = new GetAdminNotificationTemplatesQuery { Page = 1, PageSize = 10 };

            var result = await handler.Handle(query);

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
        }
    }
}