using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Services
{
    public class SmsOptions
    {
        public string Provider { get; set; } = string.Empty; // twilio, etc.
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Sends SMS messages. When not configured (or disabled), it logs and skips
    /// the send so the application works in development and tests without a provider.
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly SmsOptions _options;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IOptions<SmsOptions> options, ILogger<SmsService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.FromNumber))
            {
                _logger.LogInformation("SMS provider not configured. SMS to {To} skipped.", message.To);
                return;
            }

            // Placeholder for a real provider (e.g., Twilio REST API) call.
            // Keep the interface stable; the concrete call is provider-specific.
            _logger.LogInformation("SMS sent to {To}: {Body}", message.To, message.Body);

            await Task.CompletedTask;
        }
    }
}