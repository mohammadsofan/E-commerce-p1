using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class HeroBannerConfiguration : IEntityTypeConfiguration<HeroBanner>
    {
        public void Configure(EntityTypeBuilder<HeroBanner> builder)
        {
            builder.ToTable("HeroBanners");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.BadgeText)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(b => b.Subtitle)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(b => b.PrimaryButtonText)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.PrimaryButtonLink)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(b => b.SecondaryButtonText)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.SecondaryButtonLink)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(b => b.ImageUrl)
                .HasMaxLength(1000);

            builder.Property(b => b.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(b => b.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(b => b.IsActive);
            builder.HasIndex(b => b.DisplayOrder);
        }
    }
}
