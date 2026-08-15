using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IIdentityService
    {
        Task<string> GetUserNameAsync(System.Guid userId, CancellationToken cancellationToken = default);
    }
}
