using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminNotificationsQueryHandler : IQueryHandler<GetAdminNotificationsQuery, PagedResult<AdminNotificationDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminNotificationsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminNotificationDto>> Handle(GetAdminNotificationsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.Notifications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Type))
                q = q.Where(n => n.Type == query.Type);

            if (!string.IsNullOrWhiteSpace(query.Channel))
                q = q.Where(n => n.Channel == query.Channel);

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(n => n.Status == query.Status);

            if (query.UserId.HasValue)
                q = q.Where(n => n.UserId == query.UserId.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var notifications = await q
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminNotificationDto>
            {
                Items = _mapper.Map<List<AdminNotificationDto>>(notifications),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminNotificationByIdQueryHandler : IQueryHandler<GetAdminNotificationByIdQuery, AdminNotificationDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminNotificationByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationDto> Handle(GetAdminNotificationByIdQuery query, CancellationToken cancellationToken = default)
        {
            var notification = await _db.Notifications.FindAsync(new object[] { query.Id }, cancellationToken);
            if (notification == null)
                throw new Domain.Exceptions.NotFoundException("Notification", query.Id);

            return _mapper.Map<AdminNotificationDto>(notification);
        }
    }

    public class GetAdminNotificationTemplatesQueryHandler : IQueryHandler<GetAdminNotificationTemplatesQuery, PagedResult<AdminNotificationTemplateDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminNotificationTemplatesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminNotificationTemplateDto>> Handle(GetAdminNotificationTemplatesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.NotificationTemplates.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Channel))
                q = q.Where(t => t.Channel == query.Channel);

            if (query.IsActive.HasValue)
                q = q.Where(t => t.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(t => t.Name.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(cancellationToken);

            var templates = await q
                .OrderBy(t => t.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminNotificationTemplateDto>
            {
                Items = _mapper.Map<List<AdminNotificationTemplateDto>>(templates),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminNotificationTemplateByIdQueryHandler : IQueryHandler<GetAdminNotificationTemplateByIdQuery, AdminNotificationTemplateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminNotificationTemplateByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationTemplateDto> Handle(GetAdminNotificationTemplateByIdQuery query, CancellationToken cancellationToken = default)
        {
            var template = await _db.NotificationTemplates.FindAsync(new object[] { query.Id }, cancellationToken);
            if (template == null)
                throw new Domain.Exceptions.NotFoundException("NotificationTemplate", query.Id);

            return _mapper.Map<AdminNotificationTemplateDto>(template);
        }
    }

    public class GetAdminNotificationPreferencesQueryHandler : IQueryHandler<GetAdminNotificationPreferencesQuery, PagedResult<AdminNotificationPreferenceDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminNotificationPreferencesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminNotificationPreferenceDto>> Handle(GetAdminNotificationPreferencesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.NotificationPreferences.AsQueryable();

            if (query.UserId.HasValue)
                q = q.Where(p => p.UserId == query.UserId.Value);

            if (!string.IsNullOrWhiteSpace(query.NotificationType))
                q = q.Where(p => p.NotificationType == query.NotificationType);

            var totalCount = await q.CountAsync(cancellationToken);

            var preferences = await q
                .OrderBy(p => p.NotificationType)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminNotificationPreferenceDto>
            {
                Items = _mapper.Map<List<AdminNotificationPreferenceDto>>(preferences),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminNotificationChannelsQueryHandler : IQueryHandler<GetAdminNotificationChannelsQuery, PagedResult<AdminNotificationChannelDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminNotificationChannelsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminNotificationChannelDto>> Handle(GetAdminNotificationChannelsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.NotificationChannels.AsQueryable();

            if (query.IsActive.HasValue)
                q = q.Where(c => c.IsActive == query.IsActive.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var channels = await q
                .OrderBy(c => c.Priority)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminNotificationChannelDto>
            {
                Items = _mapper.Map<List<AdminNotificationChannelDto>>(channels),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminNotificationChannelByIdQueryHandler : IQueryHandler<GetAdminNotificationChannelByIdQuery, AdminNotificationChannelDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminNotificationChannelByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationChannelDto> Handle(GetAdminNotificationChannelByIdQuery query, CancellationToken cancellationToken = default)
        {
            var channel = await _db.NotificationChannels.FindAsync(new object[] { query.Id }, cancellationToken);
            if (channel == null)
                throw new Domain.Exceptions.NotFoundException("NotificationChannel", query.Id);

            return _mapper.Map<AdminNotificationChannelDto>(channel);
        }
    }
}