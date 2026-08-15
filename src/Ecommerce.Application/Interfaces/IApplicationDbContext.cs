using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
