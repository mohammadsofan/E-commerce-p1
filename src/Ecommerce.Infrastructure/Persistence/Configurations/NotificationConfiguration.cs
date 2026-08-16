using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Channel).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Subject).HasMaxLength(200);
            builder.Property(x => x.Body).IsRequired();
            builder.Property(x => x.DataJson).HasMaxLength(4000);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
            builder.Property(x => x.ProviderMessageId).HasMaxLength(100);
            builder.Property(x => x.ErrorMessage).HasMaxLength(1000);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.Channel);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);
        }
    }

    public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
    {
        public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.HasIndex(x => x.Name).IsUnique();

            builder.Property(x => x.Channel).IsRequired().HasMaxLength(20);
            builder.Property(x => x.SubjectTemplate).HasMaxLength(200);
            builder.Property(x => x.BodyTemplate).IsRequired();
            builder.Property(x => x.VariablesJson).HasMaxLength(2000);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasIndex(x => x.Channel);
            builder.HasIndex(x => x.IsActive);
        }
    }

    public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
    {
        public void Configure(EntityTypeBuilder<NotificationPreference> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.NotificationType).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Channel).IsRequired().HasMaxLength(20);

            builder.HasIndex(x => new { x.UserId, x.NotificationType, x.Channel }).IsUnique();
            builder.HasIndex(x => x.UserId);
        }
    }

    public class NotificationChannelConfiguration : IEntityTypeConfiguration<NotificationChannel>
    {
        public void Configure(EntityTypeBuilder<NotificationChannel> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
            builder.Property(x => x.ConfigurationJson).IsRequired();

            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Priority);
        }
    }
}