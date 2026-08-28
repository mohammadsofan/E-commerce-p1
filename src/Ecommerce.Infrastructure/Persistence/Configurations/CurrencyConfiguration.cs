using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
    {
        public void Configure(EntityTypeBuilder<Currency> builder)
        {
            builder.HasIndex(c => c.IsBaseCurrency)
                .HasDatabaseName("IX_Currencies_IsBaseCurrency_Unique")
                .HasFilter("[IsBaseCurrency] = 1")
                .IsUnique();
        }
    }
}
