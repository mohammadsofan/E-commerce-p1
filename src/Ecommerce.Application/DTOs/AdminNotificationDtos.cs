using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class AdminNotificationDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string DataJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ProviderMessageId { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class AdminNotificationTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string SubjectTemplate { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
        public string VariablesJson { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class AdminNotificationPreferenceDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class AdminNotificationChannelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ConfigurationJson { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}