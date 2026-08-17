using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateNotificationCommand : ICommand<AdminNotificationDto>
    {
        public Guid? UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string DataJson { get; set; } = string.Empty;
    }

    public class UpdateNotificationCommand : ICommand<AdminNotificationDto>
    {
        public Guid Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string DataJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class DeleteNotificationCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }

    public class CreateNotificationTemplateCommand : ICommand<AdminNotificationTemplateDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string SubjectTemplate { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
        public string VariablesJson { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateNotificationTemplateCommand : ICommand<AdminNotificationTemplateDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string SubjectTemplate { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
        public string VariablesJson { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class DeleteNotificationTemplateCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }

    public class UpdateNotificationPreferenceCommand : ICommand<AdminNotificationPreferenceDto>
    {
        public Guid Id { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class CreateNotificationChannelCommand : ICommand<AdminNotificationChannelDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ConfigurationJson { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; }
    }

    public class UpdateNotificationChannelCommand : ICommand<AdminNotificationChannelDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ConfigurationJson { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Priority { get; set; }
    }

    public class DeleteNotificationChannelCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}