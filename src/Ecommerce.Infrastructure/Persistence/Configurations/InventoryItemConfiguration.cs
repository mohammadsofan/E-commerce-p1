using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
    {
        public void Configure(EntityTypeBuilder<InventoryItem> builder)
        {
            builder.ToTable("InventoryItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuantityOnHand).IsRequired();
            builder.Property(x => x.QuantityReserved).IsRequired();
            builder.Property(x => x.ReorderLevel).IsRequired();
            builder.Property(x => x.ReorderQuantity).IsRequired();
            builder.Property(x => x.AllowBackorder).IsRequired();

            builder.Property(x => x.UpdatedAt).IsRequired();

            // RowVersion for optimistic concurrency
                 builder.Property(x => x.RowVersion)
                     .IsRequired(false)
                     .IsRowVersion()
                     .IsConcurrencyToken();

            // Computed / derived property - ignore in EF mapping
            builder.Ignore(x => x.Available);

            // Foreign keys - Restrict delete to avoid SQL Server "multiple cascade paths"
            // cycle error between Product/ProductVariant/InventoryItem.
            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ProductVariant)
                .WithMany()
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
