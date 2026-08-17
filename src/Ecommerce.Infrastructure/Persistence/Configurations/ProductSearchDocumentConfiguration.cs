using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ProductSearchDocumentConfiguration : IEntityTypeConfiguration<ProductSearchDocument>
    {
        public void Configure(EntityTypeBuilder<ProductSearchDocument> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Slug).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Sku).HasMaxLength(100);
            builder.Property(x => x.ShortDescription).HasMaxLength(500);
            builder.Property(x => x.SearchText).HasMaxLength(2000);
            builder.Property(x => x.BasePrice).HasPrecision(18, 2);

            builder.HasIndex(x => x.ProductId).IsUnique();
            builder.HasIndex(x => x.Sku);
        }
    }
}