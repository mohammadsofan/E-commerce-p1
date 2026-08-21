using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.VariantName).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.SelectedOptions).HasMaxLength(1024).IsRequired(false);
            builder.Property(x => x.Sku).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.ProductImageUrl).IsRequired(false);

            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

            builder.HasIndex(x => x.OrderId);
        }
    }
}
