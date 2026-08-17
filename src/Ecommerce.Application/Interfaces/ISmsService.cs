using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public class SmsMessage
    {
        public string To { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public interface ISmsService
    {
        Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
    }
}