using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string? ToName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
        public Dictionary<string, string>? Attachments { get; set; }
    }

    public interface IEmailService
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
        Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default);
    }
}