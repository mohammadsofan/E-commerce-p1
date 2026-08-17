using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
            builder.Property(x => x.ProviderPaymentId).IsRequired().HasMaxLength(100);
            builder.HasIndex(x => x.ProviderPaymentId).IsUnique();

            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.Property(x => x.PaymentMethod).HasMaxLength(50);
            builder.Property(x => x.FailureReason).HasMaxLength(500);
            builder.Property(x => x.RefundedAmount).HasPrecision(18, 2);
            builder.Property(x => x.CapturedAmount).HasPrecision(18, 2);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasOne<Order>()
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Refunds)
                .WithOne(r => r.Payment)
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ProviderPaymentId);
        }
    }

    public class RefundConfiguration : IEntityTypeConfiguration<Refund>
    {
        public void Configure(EntityTypeBuilder<Refund> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProviderRefundId).HasMaxLength(100);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.Property(x => x.FailureReason).HasMaxLength(500);

            builder.HasIndex(x => x.PaymentId);
            builder.HasIndex(x => x.ProviderRefundId);
            builder.HasIndex(x => x.Status);
        }
    }
}