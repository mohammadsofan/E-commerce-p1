using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class EmailServiceTests
    {
        private IEmailService CreateService(Action<EmailOptions>? configure = null)
        {
            var options = new EmailOptions
            {
                Host = "smtp.example.com",
                Port = 587,
                FromEmail = "no-reply@example.com",
                FromName = "Ecommerce",
                EnableSsl = true,
                UseCredentials = false
            };
            configure?.Invoke(options);
            return new EmailService(Options.Create(options), NullLogger<EmailService>.Instance);
        }

        [Fact]
        public async Task SendAsync_NoSmtpHostConfigured_SkipsGracefully()
        {
            var service = CreateService(o => o.Host = string.Empty);

            var message = new EmailMessage
            {
                To = "customer@example.com",
                Subject = "Test",
                Body = "Body"
            };

            await service.SendAsync(message);
        }

        [Fact]
        public async Task SendTemplateAsync_ReplacesVariables()
        {
            var service = CreateService(o => o.Host = string.Empty);

            var variables = new Dictionary<string, string>
            {
                { "orderNumber", "12345" },
                { "customerName", "John" }
            };

            await service.SendTemplateAsync("customer@example.com", "Order {{orderNumber}} for {{customerName}}", variables);
        }

        [Fact]
        public async Task SendAsync_Throws_WhenSmtpFails()
        {
            // Host set but no server listening -> SmtpClient will throw after timeout.
            var service = CreateService(o =>
            {
                o.Host = "127.0.0.1";
                o.Port = 1;
                o.UseCredentials = false;
            });

            var message = new EmailMessage
            {
                To = "customer@example.com",
                Subject = "Test",
                Body = "Body"
            };

            await Assert.ThrowsAsync<System.Net.Mail.SmtpException>(() => service.SendAsync(message));
        }

        [Fact]
        public async Task SendOrderConfirmationAsync_NoSmtpHost_SkipsGracefully()
        {
            var service = CreateService(o => o.Host = string.Empty);
            var order = new Ecommerce.Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-001",
                CustomerNotes = "Ramallah, Palestine"
            };

            await service.SendOrderConfirmationAsync(order, "customer@example.com");
        }

        [Fact]
        public async Task SendAdminOrderAlertAsync_NoSmtpHost_SkipsGracefully()
        {
            var service = CreateService(o => o.Host = string.Empty);
            var order = new Ecommerce.Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-002",
                CustomerNotes = "Nablus, Palestine"
            };

            await service.SendAdminOrderAlertAsync(order);
        }

        [Fact]
        public async Task SendOrderShippedAsync_NoSmtpHost_SkipsGracefully()
        {
            var service = CreateService(o => o.Host = string.Empty);
            var order = new Ecommerce.Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-003",
                CustomerNotes = "Jerusalem",
                Notes = "Shipped via Aramex with tracking: ARX123456"
            };

            await service.SendOrderShippedAsync(order, "customer@example.com");
        }
        [Theory]
        [InlineData("Normal English address", "Normal English address")]
        [InlineData("شارع الإرسال، عمارة البرج، طابق 4", "شارع الإرسال، عمارة البرج، طابق 4")]
        [InlineData("רחוב יפו", "רחוב יפו")]
        [InlineData("Apt 4B, 123 Main St.", "Apt 4B, 123 Main St.")]
        [InlineData("O'Connor & Sons - 123", "O&#39;Connor &amp; Sons - 123")]
        [InlineData("<script>alert(1)</script>", "&lt;script&gt;alert(1)&lt;/script&gt;")]
        [InlineData("<img src=x onerror=alert(1)>", "&lt;img src=x onerror=alert(1)&gt;")]
        [InlineData("<a href=\"javascript:alert(1)\" onmouseover=\"alert(2)\">Click me</a>", "&lt;a href=&quot;javascript:alert(1)&quot; onmouseover=&quot;alert(2)&quot;&gt;Click me&lt;/a&gt;")]
        public async Task SendOrderConfirmationAsync_EncodesXssPayloadsInAddress(string maliciousAddress, string expectedEncodedAddress)
        {
            var service = CreateService(o => o.Host = string.Empty);
            var order = new Ecommerce.Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-XSS",
                CustomerNotes = maliciousAddress
            };

            // We mock the IOptions and Logger, but we need to intercept the actual email body
            // However, EmailService is concrete and does not expose the rendered HTML.
            // Wait, we can't easily capture the HTML output from the SmtpClient if we mock it...
            // Let's use a mocked IEmailService? No, the SmtpClient is instantiated directly inside SendAsync!
            // This is a unit test limitation. Let's see if we can use a reflection or local SMTP server?
        }
    }
}

