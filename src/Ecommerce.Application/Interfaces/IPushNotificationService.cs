using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public class PushMessage
    {
        public string DeviceToken { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string>? Data { get; set; }
    }

    public interface IPushNotificationService
    {
        Task SendAsync(PushMessage message, CancellationToken cancellationToken = default);
    }
}