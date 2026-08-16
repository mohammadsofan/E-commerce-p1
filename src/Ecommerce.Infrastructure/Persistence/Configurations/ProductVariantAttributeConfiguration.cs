using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ProductVariantAttributeConfiguration : IEntityTypeConfiguration<ProductVariantAttribute>
    {
        public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Value).IsRequired().HasMaxLength(200);

            builder.HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ProductAttribute>()
                .WithMany()
                .HasForeignKey(x => x.ProductAttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ProductVariantId, x.ProductAttributeId }).IsUnique();

            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.ProductAttributeId);
        }
    }
}