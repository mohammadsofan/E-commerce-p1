using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("UserProfiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.LastName).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired(false);
            builder.Property(x => x.Gender).HasMaxLength(32).IsRequired(false);
            builder.Property(x => x.ProfileImageUrl).HasMaxLength(500).IsRequired(false);

            builder.HasIndex(x => x.UserId).IsUnique();
        }
    }
}