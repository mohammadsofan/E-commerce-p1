using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Services
{
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool UseCredentials { get; set; } = true;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Host))
            {
                _logger.LogWarning("SMTP host not configured. Email to {To} skipped.", message.To);
                return;
            }

            using var smtp = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (_options.UseCredentials && !string.IsNullOrWhiteSpace(_options.Username))
            {
                smtp.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            var from = string.IsNullOrWhiteSpace(_options.FromName)
                ? new MailAddress(_options.FromEmail)
                : new MailAddress(_options.FromEmail, _options.FromName);

            var mail = new MailMessage
            {
                From = from,
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };
            if (string.IsNullOrWhiteSpace(message.ToName))
            {
                mail.To.Add(message.To);
            }
            else
            {
                mail.To.Add(new MailAddress(message.To, message.ToName));
            }

            if (message.Cc != null)
                foreach (var cc in message.Cc)
                    mail.CC.Add(cc);

            if (message.Bcc != null)
                foreach (var bcc in message.Bcc)
                    mail.Bcc.Add(bcc);

            try
            {
                await smtp.SendMailAsync(mail, cancellationToken);
                _logger.LogInformation("Email sent to {To} with subject {Subject}", message.To, message.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", message.To, message.Subject);
                throw;
            }
            finally
            {
                mail.Dispose();
            }
        }

        public async Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
        {
            var subject = templateName;
            var body = templateName;

            if (variables != null)
            {
                foreach (var kvp in variables)
                {
                    subject = subject.Replace("{{" + kvp.Key + "}}", kvp.Value);
                    body = body.Replace("{{" + kvp.Key + "}}", kvp.Value);
                }
            }

            await SendAsync(new EmailMessage
            {
                To = to,
                Subject = subject,
                Body = body
            }, cancellationToken);
        }
    }
}