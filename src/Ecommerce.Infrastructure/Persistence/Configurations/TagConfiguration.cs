using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tags");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();

            builder.HasIndex(x => x.Slug).IsUnique();
        }
    }
}