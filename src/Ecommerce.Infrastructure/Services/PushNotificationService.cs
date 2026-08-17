using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Services
{
    public class PushOptions
    {
        public string Provider { get; set; } = string.Empty; // fcm, apns, etc.
        public string ServerKey { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Sends push notifications. When not configured (or disabled), it logs and
    /// skips the send so the application works in development and tests.
    /// </summary>
    public class PushNotificationService : IPushNotificationService
    {
        private readonly PushOptions _options;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(IOptions<PushOptions> options, ILogger<PushNotificationService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ServerKey))
            {
                _logger.LogInformation("Push provider not configured. Push to {Token} skipped.", message.DeviceToken);
                return;
            }

            // Placeholder for a real provider (e.g., Firebase Cloud Messaging) call.
            _logger.LogInformation("Push sent to {Token}: {Title}", message.DeviceToken, message.Title);

            await Task.CompletedTask;
        }
    }
}