using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class VendorProductConfiguration : IEntityTypeConfiguration<VendorProduct>
    {
        public void Configure(EntityTypeBuilder<VendorProduct> builder)
        {
            builder.ToTable("VendorProducts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.VendorSku).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.Price).HasColumnType("decimal(18,2)");

            builder.HasOne<Vendor>()
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.VendorId);
            builder.HasIndex(x => x.ProductId);
        }
    }
}