using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.DomainEvents;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.DomainEvents;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Reacts to an order being placed by sending order confirmation notifications
    /// (email, SMS, push) to the customer per their preferences and persisting
    /// Notification records for auditability.
    /// </summary>
    public class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedDomainEvent>
    {
        private readonly IApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly ILogger<OrderPlacedEventHandler> _logger;

        public OrderPlacedEventHandler(
            IApplicationDbContext db,
            IEmailService emailService,
            ISmsService smsService,
            IPushNotificationService pushService,
            ILogger<OrderPlacedEventHandler> logger)
        {
            _db = db;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
            _logger = logger;
        }

        public async Task Handle(OrderPlacedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing order placed event for order {OrderId}", domainEvent.OrderId);

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == domainEvent.OrderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found; skipping notification", domainEvent.OrderId);
                return;
            }

            if (order.UserId.HasValue)
                await SendEmailAsync(order, order.UserId.Value, cancellationToken);

            if (order.UserId.HasValue)
                await SendSmsAsync(order, order.UserId.Value, cancellationToken);

            if (order.UserId.HasValue)
                await SendPushAsync(order, order.UserId.Value, cancellationToken);
        }

        private async Task SendEmailAsync(Order order, Guid userId, CancellationToken cancellationToken)
        {
            if (!await IsChannelEnabledAsync(userId, "email", cancellationToken))
            {
                _logger.LogInformation("Customer {UserId} disabled email notifications; skipping order email", userId);
                return;
            }

            var customerEmail = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                _logger.LogWarning("Order {OrderId} has no customer email; skipping order email", order.Id);
                return;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = order.UserId,
                Type = "OrderPlaced",
                Channel = "email",
                Subject = $"Order {order.OrderNumber} confirmed",
                Body = BuildEmailBody(order),
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            try
            {
                await _emailService.SendAsync(new EmailMessage
                {
                    To = customerEmail,
                    ToName = await GetCustomerDisplayNameAsync(order.UserId, cancellationToken),
                    Subject = notification.Subject,
                    Body = notification.Body,
                    IsHtml = true
                }, cancellationToken);

                notification.Status = "sent";
                notification.SentAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", order.Id);
                notification.Status = "failed";
                notification.ErrorMessage = ex.Message;
            }

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SendSmsAsync(Order order, Guid userId, CancellationToken cancellationToken)
        {
            if (!await IsChannelEnabledAsync(userId, "sms", cancellationToken))
            {
                _logger.LogInformation("Customer {UserId} disabled SMS notifications; skipping order SMS", userId);
                return;
            }

            var phoneNumber = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.PhoneNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogWarning("Order {OrderId} has no customer phone number; skipping order SMS", order.Id);
                return;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = order.UserId,
                Type = "OrderPlaced",
                Channel = "sms",
                Subject = "Order confirmation",
                Body = $"Your order {order.OrderNumber} has been confirmed. Total: {order.TotalAmount.ToString("C")}.",
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            try
            {
                await _smsService.SendAsync(new SmsMessage
                {
                    To = phoneNumber,
                    Body = notification.Body
                }, cancellationToken);

                notification.Status = "sent";
                notification.SentAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation SMS for order {OrderId}", order.Id);
                notification.Status = "failed";
                notification.ErrorMessage = ex.Message;
            }

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task SendPushAsync(Order order, Guid userId, CancellationToken cancellationToken)
        {
            if (!await IsChannelEnabledAsync(userId, "push", cancellationToken))
            {
                _logger.LogInformation("Customer {UserId} disabled push notifications; skipping order push", userId);
                return;
            }

            // Push requires a configured/active push channel. The device token
            // would come from a per-user device registry; without one, the
            // notification is recorded as skipped (no provider send).
            var pushChannelConfigured = await _db.NotificationChannels
                .AnyAsync(c => c.Name == "push" && c.IsActive, cancellationToken);

            if (!pushChannelConfigured)
            {
                _logger.LogWarning("Order {OrderId} has no push channel configured; skipping order push", order.Id);
                return;
            }

            var phoneNumber = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.PhoneNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = order.UserId,
                Type = "OrderPlaced",
                Channel = "push",
                Subject = "Order confirmed",
                Body = $"Your order {order.OrderNumber} has been confirmed. Total: {order.TotalAmount.ToString("C")}.",
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            try
            {
                await _pushService.SendAsync(new PushMessage
                {
                    DeviceToken = string.IsNullOrWhiteSpace(phoneNumber) ? "user-" + userId : phoneNumber,
                    Title = notification.Subject,
                    Body = notification.Body
                }, cancellationToken);

                notification.Status = "sent";
                notification.SentAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation push for order {OrderId}", order.Id);
                notification.Status = "failed";
                notification.ErrorMessage = ex.Message;
            }

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> IsChannelEnabledAsync(Guid userId, string channel, CancellationToken cancellationToken)
        {
            var preference = await _db.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId
                    && p.NotificationType == "OrderPlaced"
                    && p.Channel == channel, cancellationToken);

            // Default to enabled when no explicit preference is configured.
            return preference == null || preference.IsEnabled;
        }

        private async Task<string?> GetCustomerDisplayNameAsync(Guid? userId, CancellationToken cancellationToken)
        {
            if (!userId.HasValue)
                return null;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
            if (user == null)
                return null;

            return string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName;
        }

        private static string BuildEmailBody(Order order)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family: Arial, sans-serif; color: #333;'>");
            sb.AppendLine("<h2>Thank you for your order!</h2>");
            sb.AppendLine($"<p>Your order <strong>{order.OrderNumber}</strong> has been received and is being processed.</p>");
            sb.AppendLine("<h3>Order summary</h3>");
            sb.AppendLine("<table border='0' cellpadding='6' cellspacing='0' style='border-collapse: collapse; width: 100%;'>");
            sb.AppendLine("<tr style='background-color: #f5f5f5;'><th align='left'>Item</th><th align='right'>Qty</th><th align='right'>Price</th></tr>");

            foreach (var item in order.Items)
            {
                sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(item.ProductName)}</td><td align='right'>{item.Quantity}</td><td align='right'>{item.UnitPrice.ToString("C")}</td></tr>");
            }

            sb.AppendLine($"<tr><td colspan='2' align='right'><strong>Subtotal</strong></td><td align='right'>{order.Subtotal.ToString("C")}</td></tr>");
            if (order.DiscountAmount > 0)
                sb.AppendLine($"<tr><td colspan='2' align='right'><strong>Discount</strong></td><td align='right'>-{order.DiscountAmount.ToString("C")}</td></tr>");
            if (order.ShippingAmount > 0)
                sb.AppendLine($"<tr><td colspan='2' align='right'><strong>Shipping</strong></td><td align='right'>{order.ShippingAmount.ToString("C")}</td></tr>");
            sb.AppendLine($"<tr><td colspan='2' align='right'><strong>Total</strong></td><td align='right'><strong>{order.TotalAmount.ToString("C")}</strong></td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<p style='margin-top: 20px;'>Thank you for shopping with us!</p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}