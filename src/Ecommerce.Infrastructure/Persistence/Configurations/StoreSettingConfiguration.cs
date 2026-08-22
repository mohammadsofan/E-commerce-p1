using System;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class StoreSettingConfiguration : IEntityTypeConfiguration<StoreSetting>
    {
        public void Configure(EntityTypeBuilder<StoreSetting> builder)
        {
            builder.ToTable("StoreSettings");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.StandardShippingCost).HasPrecision(18, 2).HasDefaultValue(15m);
            builder.Property(s => s.FreeShippingThreshold).HasPrecision(18, 2).HasDefaultValue(50m);
            builder.Property(s => s.StoreName).HasMaxLength(200).HasDefaultValue("Sofan Store");
            builder.Property(s => s.ContactEmail).HasMaxLength(200);
            builder.Property(s => s.ContactPhone).HasMaxLength(50);
            builder.Property(s => s.CurrencyCode).HasMaxLength(10).HasDefaultValue("ILS");

            builder.HasData(new StoreSetting
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                StandardShippingCost = 15m,
                FreeShippingThreshold = 50m,
                StoreName = "Sofan Store",
                ContactEmail = "mohammad.n.sofan@gmail.com",
                ContactPhone = "+970599000000",
                CurrencyCode = "ILS",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        }
    }
}
