using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
    {
        public void Configure(EntityTypeBuilder<ProductReview> builder)
        {
            builder.ToTable("ProductReviews");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).HasMaxLength(200).IsRequired(false);
            builder.Property(x => x.Comment).HasMaxLength(4000).IsRequired(false);

            builder.Property(x => x.Rating).IsRequired();

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.UserId);
        }
    }
}