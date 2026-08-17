using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.ToTable("Vendors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(64).IsRequired();

            builder.HasIndex(x => x.Code).IsUnique();
        }
    }
}