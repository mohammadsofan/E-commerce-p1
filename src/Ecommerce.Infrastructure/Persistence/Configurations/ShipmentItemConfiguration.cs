using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
    {
        public void Configure(EntityTypeBuilder<ShipmentItem> builder)
        {
            builder.ToTable("ShipmentItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).IsRequired();

            builder.HasOne<OrderItem>()
                .WithMany()
                .HasForeignKey(x => x.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<InventoryItem>()
                .WithMany()
                .HasForeignKey(x => x.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ShipmentId);
            builder.HasIndex(x => x.OrderItemId);
        }
    }
}