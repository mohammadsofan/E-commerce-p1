using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Identity;

namespace Ecommerce.Infrastructure.Persistence
{
    // Infrastructure DbContext: EF Core-specific (placed in Infrastructure)
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets (add as needed)
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductVariantAttribute> ProductVariantAttributes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<StoreFeature> StoreFeatures { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionUsage> PromotionUsages { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<ShippingZone> ShippingZones { get; set; }
        public DbSet<ShippingZoneLocation> ShippingZoneLocations { get; set; }
        public DbSet<ShippingMethod> ShippingMethods { get; set; }
        public DbSet<ShippingRate> ShippingRates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<NotificationChannel> NotificationChannels { get; set; }
        public DbSet<ProductSearchDocument> ProductSearchDocuments { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentItem> ShipmentItems { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportTicketMessage> SupportTicketMessages { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<VendorProduct> VendorProducts { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<HeroBanner> HeroBanners { get; set; }
        public DbSet<StoreSetting> StoreSettings { get; set; }

        public IQueryable<IApplicationUser> Users => base.Users.Cast<IApplicationUser>();

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class
        {
            return Entry(entity);
        }

        public void ClearChangeTracker()
        {
            ChangeTracker.Clear();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map custom properties on ApplicationUser
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.IsEmailVerified)
                .HasDefaultValue(false);
            
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.IsPhoneVerified)
                .HasDefaultValue(false);
            
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            // Apply EF Core configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
