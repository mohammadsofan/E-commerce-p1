using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");
            builder.HasKey(x => x.Id);

            // Computed; not persisted.
            builder.Ignore(x => x.LineTotal);

            builder.Property(x => x.CartId);
            builder.Property(x => x.ProductId);
            builder.Property(x => x.ProductVariantId);
            builder.Property(x => x.ProductName).HasMaxLength(256);
            builder.Property(x => x.Quantity);
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
        }
    }
}
