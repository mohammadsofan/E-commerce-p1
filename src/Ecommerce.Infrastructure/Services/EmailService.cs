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
        public string AdminEmail { get; set; } = string.Empty;
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

        public virtual async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
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
                var password = _options.Password?.Replace(" ", "").Trim() ?? string.Empty;
                smtp.Credentials = new NetworkCredential(_options.Username.Trim(), password);
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

        public async Task SendOrderConfirmationAsync(Ecommerce.Domain.Entities.Order order, string customerEmail, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                _logger.LogWarning("Cannot send order confirmation: customer email is empty for Order {OrderId}", order.Id);
                return;
            }

            var subject = $"تأكيد استلام طلبك رقم #{order.OrderNumber} - متجر سُوفان";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 20px; direction: rtl; text-align: right; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05); border: 1px solid #e2e8f0; }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #4f46e5, #7c3aed); color: #ffffff; padding: 24px; text-align: center; }");
            sb.AppendLine(".content { padding: 24px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 16px 0; }");
            sb.AppendLine("th { background-color: #f1f5f9; padding: 10px; font-weight: 600; font-size: 13px; text-align: right; border-bottom: 2px solid #e2e8f0; }");
            sb.AppendLine("td { padding: 10px; font-size: 13px; border-bottom: 1px solid #f1f5f9; text-align: right; }");
            sb.AppendLine(".badge { display: inline-block; padding: 3px 8px; border-radius: 6px; font-size: 11px; font-weight: bold; background: #ecfdf5; color: #047857; }");
            sb.AppendLine(".summary-row { font-weight: bold; }");
            sb.AppendLine(".total-row { font-size: 16px; color: #4f46e5; border-top: 2px solid #cbd5e1; }");
            sb.AppendLine(".footer { background: #f8fafc; padding: 16px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>متجر سُوفان | Sofan Store</h1>");
            sb.AppendLine("<p style='margin: 0; font-size: 16px;'>تم تأكيد طلبك بنجاح!</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<p>مرحباً،</p>");
            sb.AppendLine($"<p>شكراً لتسوقك معنا. تم استلام طلبك رقم <strong>#{order.OrderNumber}</strong> بنجاح وجارٍ تجهيزه بعناية.</p>");

            sb.AppendLine("<h3>تفاصيل المنتجات:</h3>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr><th>المنتج</th><th style='text-align:center;'>الكمية</th><th style='text-align:left;'>السعر</th><th style='text-align:left;'>المجموع</th></tr></thead><tbody>");

            foreach (var item in order.Items)
            {
                var optionsText = !string.IsNullOrWhiteSpace(item.SelectedOptions) ? $"<br/><span style='font-size:11px; color:#64748b;'>{System.Net.WebUtility.HtmlEncode(item.SelectedOptions)}</span>" : "";
                sb.AppendLine($"<tr><td><strong>{System.Net.WebUtility.HtmlEncode(item.ProductName)}</strong>{optionsText}</td><td style='text-align:center;'>{item.Quantity}</td><td style='text-align:left;'>${item.UnitPrice:F2}</td><td style='text-align:left;'>${(item.UnitPrice * item.Quantity):F2}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<h3>ملخص الطلب:</h3>");
            sb.AppendLine("<table><tbody>");
            sb.AppendLine($"<tr><td>المجموع الفرعي</td><td style='text-align:left;'>${order.Subtotal:F2}</td></tr>");

            if (order.DiscountAmount > 0)
            {
                var couponTag = !string.IsNullOrWhiteSpace(order.CouponCode) ? $" ({System.Net.WebUtility.HtmlEncode(order.CouponCode)})" : "";
                sb.AppendLine($"<tr style='color: #059669;'><td>الخصم{couponTag}</td><td style='text-align:left;'>-${order.DiscountAmount:F2}</td></tr>");
            }

            if (order.ShippingAmount > 0)
            {
                sb.AppendLine($"<tr><td>رسوم الشحن والتوصيل</td><td style='text-align:left;'>${order.ShippingAmount:F2}</td></tr>");
            }
            else
            {
                sb.AppendLine("<tr><td>الشحن</td><td style='text-align:left;'><span class='badge'>شحن مجاني</span></td></tr>");
            }

            sb.AppendLine($"<tr class='total-row'><td><strong>المبلغ الإجمالي</strong></td><td style='text-align:left;'><strong>${order.TotalAmount:F2}</strong></td></tr>");
            sb.AppendLine("</tbody></table>");

            var deliveryInfo = !string.IsNullOrWhiteSpace(order.CustomerNotes) ? order.CustomerNotes : order.Notes;
            if (!string.IsNullOrWhiteSpace(deliveryInfo))
            {
                sb.AppendLine("<h3>بيانات التوصيل والعنوان:</h3>");
                sb.AppendLine($"<p style='background: #f8fafc; padding: 12px; border-radius: 8px; border: 1px solid #e2e8f0; font-size: 13px;'>{System.Net.WebUtility.HtmlEncode(deliveryInfo)}</p>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>إذا كان لديك أي استفسار حول طلبك، يمكنك الرد مباشرة على هذه الرسالة.</p>");
            sb.AppendLine("<p>© متجر سُوفان - جميع الحقوق محفوظة</p>");
            sb.AppendLine("</div></div></body></html>");

            await SendAsync(new EmailMessage
            {
                To = customerEmail,
                Subject = subject,
                Body = sb.ToString(),
                IsHtml = true
            }, cancellationToken);
        }

        public async Task SendAdminOrderAlertAsync(Ecommerce.Domain.Entities.Order order, CancellationToken cancellationToken = default)
        {
            var adminTarget = !string.IsNullOrWhiteSpace(_options.AdminEmail) ? _options.AdminEmail : _options.FromEmail;
            if (string.IsNullOrWhiteSpace(adminTarget))
            {
                _logger.LogWarning("Admin email not configured. Skipping admin order alert for Order {OrderId}", order.Id);
                return;
            }

            var subject = $"🔔 طلب جديد وارد: #{order.OrderNumber} بقيمة ${order.TotalAmount:F2}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 20px; direction: rtl; text-align: right; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05); border: 1px solid #e2e8f0; }");
            sb.AppendLine(".header { background: #0f172a; color: #ffffff; padding: 20px; text-align: center; }");
            sb.AppendLine(".content { padding: 24px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 16px 0; }");
            sb.AppendLine("th { background-color: #f1f5f9; padding: 10px; font-weight: 600; font-size: 13px; text-align: right; border-bottom: 2px solid #e2e8f0; }");
            sb.AppendLine("td { padding: 10px; font-size: 13px; border-bottom: 1px solid #f1f5f9; text-align: right; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h2 style='margin:0;'>لوحة تحكم المتجر | تنبيه طلب جديد</h2>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<p>تم تسجيل طلب جديد برقم: <strong>#{order.OrderNumber}</strong></p>");
            sb.AppendLine($"<p><strong>تاريخ الطلب:</strong> {order.CreatedAt:yyyy-MM-dd HH:mm} UTC</p>");
            sb.AppendLine($"<p><strong>المبلغ الإجمالي:</strong> <span style='font-size:18px; color:#4f46e5; font-weight:bold;'>${order.TotalAmount:F2}</span></p>");

            var adminDeliveryInfo = !string.IsNullOrWhiteSpace(order.CustomerNotes) ? order.CustomerNotes : order.Notes;
            if (!string.IsNullOrWhiteSpace(adminDeliveryInfo))
            {
                sb.AppendLine($"<p><strong>عنوان التوصيل وبيانات التواصل:</strong><br/>{System.Net.WebUtility.HtmlEncode(adminDeliveryInfo)}</p>");
            }

            sb.AppendLine("<h3>المنتجات المطلوبة:</h3>");
            sb.AppendLine("<table><thead><tr><th>المنتج</th><th>الكمية</th><th>السعر</th></tr></thead><tbody>");
            foreach (var item in order.Items)
            {
                var options = !string.IsNullOrWhiteSpace(item.SelectedOptions) ? $" ({System.Net.WebUtility.HtmlEncode(item.SelectedOptions)})" : "";
                sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(item.ProductName)}{options}</td><td>{item.Quantity}</td><td>${item.UnitPrice:F2}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("</div></div></body></html>");

            await SendAsync(new EmailMessage
            {
                To = adminTarget,
                Subject = subject,
                Body = sb.ToString(),
                IsHtml = true
            }, cancellationToken);
        }

        public async Task SendOrderShippedAsync(Ecommerce.Domain.Entities.Order order, string customerEmail, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                _logger.LogWarning("Cannot send order shipped email: customer email is empty for Order {OrderId}", order.Id);
                return;
            }

            var subject = $"🚚 تم شحن طلبك رقم #{order.OrderNumber} - متجر سُوفان";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 20px; direction: rtl; text-align: right; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05); border: 1px solid #e2e8f0; }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #0284c7, #0369a1); color: #ffffff; padding: 24px; text-align: center; }");
            sb.AppendLine(".content { padding: 24px; }");
            sb.AppendLine(".tracking-box { background: #f0f9ff; border: 1px solid #bae6fd; padding: 16px; border-radius: 8px; margin: 16px 0; }");
            sb.AppendLine(".footer { background: #f8fafc; padding: 16px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>متجر سُوفان | Sofan Store</h1>");
            sb.AppendLine("<p style='margin: 0; font-size: 16px;'>طلبك في الطريق إليك الآن!</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<p>مرحباً،</p>");
            sb.AppendLine($"<p>يسعدنا إبلاغك بأنه تم شحن وتسليم طلبك رقم <strong>#{order.OrderNumber}</strong> إلى شركة الشحن والتوصيل.</p>");

            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                sb.AppendLine("<div class='tracking-box'>");
                sb.AppendLine($"<p style='margin:4px 0;'><strong>بيانات الشحن:</strong> <span style='font-family:monospace; font-weight:bold;'>{System.Net.WebUtility.HtmlEncode(order.Notes)}</span></p>");
                sb.AppendLine("</div>");
            }

            var shippedDeliveryInfo = !string.IsNullOrWhiteSpace(order.CustomerNotes) ? order.CustomerNotes : order.Notes;
            if (!string.IsNullOrWhiteSpace(shippedDeliveryInfo))
            {
                sb.AppendLine("<h3>العنوان المسجل للتسليم:</h3>");
                sb.AppendLine($"<p style='background: #f8fafc; padding: 12px; border-radius: 8px; border: 1px solid #e2e8f0; font-size: 13px;'>{System.Net.WebUtility.HtmlEncode(shippedDeliveryInfo)}</p>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>نتمنى لك تجربة تسوق ممتعة مع متجر سُوفان.</p>");
            sb.AppendLine("<p>© متجر سُوفان - جميع الحقوق محفوظة</p>");
            sb.AppendLine("</div></div></body></html>");

            await SendAsync(new EmailMessage
            {
                To = customerEmail,
                Subject = subject,
                Body = sb.ToString(),
                IsHtml = true
            }, cancellationToken);
        }
    }
}