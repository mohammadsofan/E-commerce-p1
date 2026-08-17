using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Sku).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.Barcode).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.Name).HasMaxLength(250).IsRequired();

            builder.Property(x => x.Price).HasPrecision(18, 2);
            builder.Property(x => x.CostPrice).HasPrecision(18, 2);
            builder.Property(x => x.CompareAtPrice).HasPrecision(18, 2);

            builder.Property(x => x.Weight).HasPrecision(18, 2);
            builder.Property(x => x.Length).HasPrecision(18, 2);
            builder.Property(x => x.Width).HasPrecision(18, 2);
            builder.Property(x => x.Height).HasPrecision(18, 2);

            builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken().IsRequired(false);
        }
    }
}
