using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ShippingZoneConfiguration : IEntityTypeConfiguration<ShippingZone>
    {
        public void Configure(EntityTypeBuilder<ShippingZone> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasMany(x => x.Locations)
                .WithOne(l => l.ShippingZone)
                .HasForeignKey(l => l.ShippingZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Methods)
                .WithOne(m => m.ShippingZone)
                .HasForeignKey(m => m.ShippingZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.IsActive);
        }
    }

    public class ShippingZoneLocationConfiguration : IEntityTypeConfiguration<ShippingZoneLocation>
    {
        public void Configure(EntityTypeBuilder<ShippingZoneLocation> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
            builder.Property(x => x.RegionCode).HasMaxLength(10);
            builder.Property(x => x.PostalCodePattern).HasMaxLength(50);

            builder.HasIndex(x => new { x.ShippingZoneId, x.CountryCode, x.RegionCode });
        }
    }

    public class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
    {
        public void Configure(EntityTypeBuilder<ShippingMethod> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(20);
            builder.Property(x => x.BaseRate).HasPrecision(18, 2);
            builder.Property(x => x.FreeShippingThreshold).HasPrecision(18, 2);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasOne(x => x.ShippingZone)
                .WithMany(z => z.Methods)
                .HasForeignKey(x => x.ShippingZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Rates)
                .WithOne(r => r.ShippingMethod)
                .HasForeignKey(r => r.ShippingMethodId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ShippingZoneId);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.DisplayOrder);
        }
    }

    public class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
    {
        public void Configure(EntityTypeBuilder<ShippingRate> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ConditionType).IsRequired().HasMaxLength(20);
            builder.Property(x => x.ConditionOperator).IsRequired().HasMaxLength(10);
            builder.Property(x => x.ConditionValueMin).HasPrecision(18, 2);
            builder.Property(x => x.ConditionValueMax).HasPrecision(18, 2);
            builder.Property(x => x.Rate).HasPrecision(18, 2);

            builder.HasOne(x => x.ShippingMethod)
                .WithMany(m => m.Rates)
                .HasForeignKey(x => x.ShippingMethodId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ShippingMethodId);
        }
    }
}