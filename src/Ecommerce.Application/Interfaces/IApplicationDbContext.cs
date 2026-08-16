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
        DbSet<ProductImage> ProductImages { get; }
        DbSet<ProductAttribute> ProductAttributes { get; }
        DbSet<ProductVariantAttribute> ProductVariantAttributes { get; }
        DbSet<Category> Categories { get; }
        DbSet<InventoryItem> InventoryItems { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<IdempotencyKey> IdempotencyKeys { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }
        DbSet<Coupon> Coupons { get; }
        DbSet<CouponUsage> CouponUsages { get; }
        DbSet<Promotion> Promotions { get; }
        DbSet<PromotionUsage> PromotionUsages { get; }
        DbSet<Payment> Payments { get; }
        DbSet<Refund> Refunds { get; }
        DbSet<TaxCategory> TaxCategories { get; }
        DbSet<TaxRate> TaxRates { get; }
        DbSet<ShippingZone> ShippingZones { get; }
        DbSet<ShippingZoneLocation> ShippingZoneLocations { get; }
        DbSet<ShippingMethod> ShippingMethods { get; }
        DbSet<ShippingRate> ShippingRates { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<NotificationTemplate> NotificationTemplates { get; }
        DbSet<NotificationPreference> NotificationPreferences { get; }
        DbSet<NotificationChannel> NotificationChannels { get; }
        IQueryable<IApplicationUser> Users { get; }

        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
