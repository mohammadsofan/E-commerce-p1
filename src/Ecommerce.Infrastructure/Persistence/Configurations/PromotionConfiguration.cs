using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
            builder.Property(x => x.RulesJson).IsRequired();

            builder.Property(x => x.ApplicableProductIds).HasMaxLength(2000);
            builder.Property(x => x.ApplicableCategoryIds).HasMaxLength(2000);
            builder.Property(x => x.ApplicableUserIds).HasMaxLength(2000);
            builder.Property(x => x.ExcludedProductIds).HasMaxLength(2000);
            builder.Property(x => x.ExcludedCategoryIds).HasMaxLength(2000);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasMany(x => x.Usages)
                .WithOne(u => u.Promotion)
                .HasForeignKey(u => u.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.StartAt);
            builder.HasIndex(x => x.EndAt);
            builder.HasIndex(x => x.Priority);
        }
    }

    public class PromotionUsageConfiguration : IEntityTypeConfiguration<PromotionUsage>
    {
        public void Configure(EntityTypeBuilder<PromotionUsage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);

            builder.HasIndex(x => x.PromotionId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.OrderId);
        }
    }
}