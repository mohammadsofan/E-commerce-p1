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

            builder.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.Status).HasMaxLength(32).IsRequired(false);
            builder.Property(x => x.PaymentStatus).HasMaxLength(32).IsRequired(false);
            builder.Property(x => x.FulfillmentStatus).HasMaxLength(32).IsRequired(false);
            builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();

            builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ShippingAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.RefundedAmount).HasColumnType("decimal(18,2)");

            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

                 builder.Property(x => x.RowVersion)
                     .IsRequired(false)
                     .IsRowVersion()
                     .IsConcurrencyToken();

                 builder.Property(x => x.CouponCode).IsRequired(false);
                 builder.Property(x => x.Notes).IsRequired(false);
                 builder.Property(x => x.CustomerNotes).IsRequired(false);

            builder.HasMany(x => x.Items)
                   .WithOne()
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
