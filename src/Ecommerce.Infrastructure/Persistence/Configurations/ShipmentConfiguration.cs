using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
    {
        public void Configure(EntityTypeBuilder<Shipment> builder)
        {
            builder.ToTable("Shipments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TrackingNumber).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.Carrier).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired(false);

            builder.HasOne<Order>()
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(i => i.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.OrderId);
        }
    }
}