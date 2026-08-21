using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Slug).IsRequired().HasMaxLength(250);
            builder.HasIndex(x => x.Slug).IsUnique();

            builder.Property(x => x.BasePrice).HasPrecision(18,2);
            builder.Property(x => x.CostPrice).HasPrecision(18,2);
            builder.Property(x => x.CompareAtPrice).HasPrecision(18,2);
            builder.Property(x => x.Weight).HasPrecision(18,2);
            builder.Property(x => x.Length).HasPrecision(18,2);
            builder.Property(x => x.Width).HasPrecision(18,2);
            builder.Property(x => x.Height).HasPrecision(18,2);
            builder.Property(x => x.AttributesJson).HasMaxLength(4000).IsRequired(false);

            builder.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}
