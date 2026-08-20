using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class StoreFeatureConfiguration : IEntityTypeConfiguration<StoreFeature>
    {
        public void Configure(EntityTypeBuilder<StoreFeature> builder)
        {
            builder.ToTable("StoreFeatures");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(f => f.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(f => f.IconName)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Truck");

            builder.Property(f => f.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(f => f.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(f => f.DisplayOrder);
            builder.HasIndex(f => f.IsActive);
        }
    }
}
