using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
            builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.OldValues).IsRequired(false);
            builder.Property(x => x.NewValues).IsRequired(false);
            builder.Property(x => x.IpAddress).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.UserAgent).HasMaxLength(500).IsRequired(false);

            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.EntityName);
        }
    }
}