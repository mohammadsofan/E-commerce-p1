using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Product> Products { get; }
        DbSet<ProductVariant> ProductVariants { get; }
        DbSet<Category> Categories { get; }
        DbSet<InventoryItem> InventoryItems { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<IdempotencyKey> IdempotencyKeys { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }
        IQueryable<IApplicationUser> Users { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
