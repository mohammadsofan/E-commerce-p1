using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>
    {
        public void Configure(EntityTypeBuilder<TaxCategory> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasMany(x => x.Rates)
                .WithOne(r => r.TaxCategory)
                .HasForeignKey(r => r.TaxCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.IsActive);
        }
    }

    public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
    {
        public void Configure(EntityTypeBuilder<TaxRate> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
            builder.Property(x => x.RegionCode).HasMaxLength(10);
            builder.Property(x => x.PostalCodePattern).HasMaxLength(50);
            builder.Property(x => x.Rate).HasPrecision(18, 4);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasOne(x => x.TaxCategory)
                .WithMany(c => c.Rates)
                .HasForeignKey(x => x.TaxCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TaxCategoryId, x.CountryCode, x.RegionCode });
            builder.HasIndex(x => x.IsActive);
        }
    }
}