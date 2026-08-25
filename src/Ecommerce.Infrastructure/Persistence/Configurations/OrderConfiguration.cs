using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(x => x.Id);

            // Domain events are transient and must not be persisted.
            builder.Ignore(x => x.DomainEvents);

            builder.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired(false);
            // Status enums are stored as their string name and are required (never null on a real order).
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.FulfillmentStatus).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();

            builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ShippingAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.RefundedAmount).HasColumnType("decimal(18,2)");

            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

                 builder.Property(x => x.RowVersion)
                     .IsRequired(false)
                     .IsRowVersion()
                     .IsConcurrencyToken();

                 builder.Property(x => x.CouponCode).IsRequired(false);
                 builder.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired(false);
                 builder.Property(x => x.Notes).IsRequired(false);
                 builder.Property(x => x.CustomerNotes).IsRequired(false);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.OrderNumber).IsUnique();
            builder.HasIndex(x => new { x.Status, x.CreatedAt });

            builder.HasMany(x => x.Items)
                   .WithOne()
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
