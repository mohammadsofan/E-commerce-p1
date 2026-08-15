using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<InventoryItem> InventoryItems { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
