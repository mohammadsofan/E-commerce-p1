using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
    {
        public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
        {
            builder.ToTable("SupportTicketMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Message).HasMaxLength(4000).IsRequired(false);
            builder.Property(x => x.IsInternal).IsRequired();

            builder.HasIndex(x => x.SupportTicketId);
            builder.HasIndex(x => x.UserId);
        }
    }
}