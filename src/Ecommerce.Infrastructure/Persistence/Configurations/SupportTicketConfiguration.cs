using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> builder)
        {
            builder.ToTable("SupportTickets");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Subject).HasMaxLength(250).IsRequired(false);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired(false);
            builder.Property(x => x.Priority).HasMaxLength(16).IsRequired(false);

            builder.HasMany(x => x.Messages)
                .WithOne()
                .HasForeignKey(m => m.SupportTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.AssignedToUserId);
            builder.HasIndex(x => x.Status);
        }
    }
}