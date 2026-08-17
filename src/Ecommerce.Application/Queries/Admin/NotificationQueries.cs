using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminNotificationsQuery : IQuery<PagedResult<AdminNotificationDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Type { get; set; }
        public string? Channel { get; set; }
        public string? Status { get; set; }
        public Guid? UserId { get; set; }
    }

    public class GetAdminNotificationByIdQuery : IQuery<AdminNotificationDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminNotificationTemplatesQuery : IQuery<PagedResult<AdminNotificationTemplateDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public string? Channel { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAdminNotificationTemplateByIdQuery : IQuery<AdminNotificationTemplateDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminNotificationPreferencesQuery : IQuery<PagedResult<AdminNotificationPreferenceDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? UserId { get; set; }
        public string? NotificationType { get; set; }
    }

    public class GetAdminNotificationChannelsQuery : IQuery<PagedResult<AdminNotificationChannelDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? IsActive { get; set; }
    }

    public class GetAdminNotificationChannelByIdQuery : IQuery<AdminNotificationChannelDto>
    {
        public Guid Id { get; set; }
    }
}