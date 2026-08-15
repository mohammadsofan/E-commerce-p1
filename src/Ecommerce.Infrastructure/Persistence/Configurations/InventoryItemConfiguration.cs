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
            builder.Property(x => x.ReorderLevel).IsRequired(false);
            builder.Property(x => x.ReorderQuantity).IsRequired(false);
            builder.Property(x => x.AllowBackorder).IsRequired();

            builder.Property(x => x.UpdatedAt).IsRequired();

            // RowVersion for optimistic concurrency
            builder.Property(x => x.RowVersion)
                   .IsRowVersion()
                   .IsConcurrencyToken();

            // Computed / derived property - ignore in EF mapping
            builder.Ignore(x => x.Available);

            // Foreign keys (if navigations exist) can be configured here by name
            // e.g., builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
        }
    }
}
