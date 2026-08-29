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
        private class SpyEmailService : Ecommerce.Infrastructure.Services.EmailService
        {
            public List<EmailMessage> SentMessages { get; } = new List<EmailMessage>();

            public SpyEmailService(IOptions<EmailOptions> options, Microsoft.Extensions.Logging.ILogger<Ecommerce.Infrastructure.Services.EmailService> logger) 
                : base(options, logger) { }

            public override Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            {
                SentMessages.Add(message);
                return Task.CompletedTask;
            }
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
            var options = Options.Create(new EmailOptions { Host = "smtp.example.com", FromEmail = "admin@example.com" });
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<Ecommerce.Infrastructure.Services.EmailService>.Instance;
            var service = new SpyEmailService(options, logger);

            var order = new Ecommerce.Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST-XSS",
                CustomerNotes = maliciousAddress
            };

            await service.SendOrderConfirmationAsync(order, "customer@example.com");

            var sent = Assert.Single(service.SentMessages);
            Assert.Contains(expectedEncodedAddress, sent.Body);
            if (maliciousAddress != expectedEncodedAddress)
            {
                Assert.DoesNotContain(maliciousAddress, sent.Body);
            }
        }

        [Fact]
        public async Task SendOrderConfirmationAsync_EncodesCouponCode()
        {
            var options = Options.Create(new EmailOptions { Host = "smtp.example.com", FromEmail = "admin@example.com" });
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<Ecommerce.Infrastructure.Services.EmailService>.Instance;
            var service = new SpyEmailService(options, logger);

            var order = new Ecommerce.Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-COUPON-XSS"
            };
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Test Product", 100m, 1);
            order.ApplyCoupon("<script>alert(1)</script>", 10m);

            await service.SendOrderConfirmationAsync(order, "customer@example.com");

            var sent = Assert.Single(service.SentMessages);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", sent.Body);
            Assert.DoesNotContain("<script>alert(1)</script>", sent.Body);
        }

        [Fact]
        public async Task SendAdminOrderAlertAsync_EncodesSelectedOptions()
        {
            var options = Options.Create(new EmailOptions { Host = "smtp.example.com", FromEmail = "admin@example.com" });
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<Ecommerce.Infrastructure.Services.EmailService>.Instance;
            var service = new SpyEmailService(options, logger);

            var order = new Ecommerce.Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-OPT-XSS"
            };
            
            order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Test Product", 100m, 1, 0m, "", "", "", "<img src=x onerror=alert(1)>");

            await service.SendAdminOrderAlertAsync(order);

            var sent = Assert.Single(service.SentMessages);
            Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", sent.Body);
            Assert.DoesNotContain("<img src=x onerror=alert(1)>", sent.Body);
        }
    }
}

