using System;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IIdempotencyService
    {
        Task<(bool Found, string Response)> TryGetResponseAsync(string key);
        Task<bool> TryRegisterAsync(string key, string requestHash, Guid ownerId);
        Task SaveResponseAsync(string key, string response);
    }
}
