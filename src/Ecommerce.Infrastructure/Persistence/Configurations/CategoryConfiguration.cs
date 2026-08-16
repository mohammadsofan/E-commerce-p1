using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Slug).IsRequired().HasMaxLength(200);
            builder.HasIndex(x => x.Slug).IsUnique();

            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.ImageUrl).HasMaxLength(500);
            builder.Property(x => x.MetaTitle).HasMaxLength(200);
            builder.Property(x => x.MetaDescription).HasMaxLength(500);

            builder.HasOne<Category>()
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}