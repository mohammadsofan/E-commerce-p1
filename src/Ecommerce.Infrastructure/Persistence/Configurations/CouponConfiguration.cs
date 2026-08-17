using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Value).HasPrecision(18, 2);
            builder.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
            builder.Property(x => x.MaxDiscountAmount).HasPrecision(18, 2);

            builder.Property(x => x.ApplicableProductIds).HasMaxLength(2000);
            builder.Property(x => x.ApplicableCategoryIds).HasMaxLength(2000);
            builder.Property(x => x.ApplicableUserIds).HasMaxLength(2000);
            builder.Property(x => x.ExcludedProductIds).HasMaxLength(2000);
            builder.Property(x => x.ExcludedCategoryIds).HasMaxLength(2000);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasMany(x => x.Usages)
                .WithOne(u => u.Coupon)
                .HasForeignKey(u => u.CouponId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.StartAt);
            builder.HasIndex(x => x.EndAt);
        }
    }

    public class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
    {
        public void Configure(EntityTypeBuilder<CouponUsage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);

            builder.HasIndex(x => x.CouponId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.OrderId);
        }
    }
}