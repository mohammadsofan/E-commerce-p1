using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("Addresses");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).HasMaxLength(32).IsRequired(false);
            builder.Property(x => x.FirstName).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.LastName).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.CompanyName).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.AddressLine1).HasMaxLength(250).IsRequired(false);
            builder.Property(x => x.AddressLine2).HasMaxLength(250).IsRequired(false);
            builder.Property(x => x.City).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.State).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.PostalCode).HasMaxLength(32).IsRequired(false);
            builder.Property(x => x.CountryCode).HasMaxLength(4).IsRequired(false);
            builder.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired(false);

            builder.HasIndex(x => x.UserId);
        }
    }
}