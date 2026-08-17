using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
    {
        public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
        {
            builder.ToTable("IdempotencyKeys");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.RequestHash)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.ResponseData)
                .IsRequired(false);

            builder.HasIndex(x => x.Key).IsUnique();
            builder.HasIndex(x => x.OwnerId);
            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}