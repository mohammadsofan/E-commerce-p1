using System;

namespace Ecommerce.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty; // email, sms, push, in_app
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string DataJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, sent, failed, delivered
        public string? ProviderMessageId { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class NotificationTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty; // email, sms, push
        public string SubjectTemplate { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
        public string VariablesJson { get; set; } = string.Empty; // JSON schema of available variables
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class NotificationPreference
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class NotificationChannel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // email, sms, push
        public string Provider { get; set; } = string.Empty; // sendgrid, twilio, firebase, etc.
        public string ConfigurationJson { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
