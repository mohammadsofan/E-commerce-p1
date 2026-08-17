using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateNotificationCommandHandler : ICommandHandler<CreateNotificationCommand, AdminNotificationDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateNotificationCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationDto> Handle(CreateNotificationCommand command, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = command.UserId,
                Type = command.Type,
                Channel = command.Channel,
                Subject = command.Subject,
                Body = command.Body,
                DataJson = command.DataJson,
                Status = "pending",
                RetryCount = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminNotificationDto>(notification);
        }
    }

    public class UpdateNotificationCommandHandler : ICommandHandler<UpdateNotificationCommand, AdminNotificationDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateNotificationCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationDto> Handle(UpdateNotificationCommand command, CancellationToken cancellationToken = default)
        {
            var notification = await _db.Notifications.FindAsync(new object[] { command.Id }, cancellationToken);
            if (notification == null)
                throw new Domain.Exceptions.NotFoundException("Notification", command.Id);

            notification.Subject = command.Subject;
            notification.Body = command.Body;
            notification.DataJson = command.DataJson;
            notification.Status = command.Status;
            notification.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminNotificationDto>(notification);
        }
    }

    public class DeleteNotificationCommandHandler : ICommandHandler<DeleteNotificationCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteNotificationCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteNotificationCommand command, CancellationToken cancellationToken = default)
        {
            var notification = await _db.Notifications.FindAsync(new object[] { command.Id }, cancellationToken);
            if (notification == null)
                throw new Domain.Exceptions.NotFoundException("Notification", command.Id);

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class CreateNotificationTemplateCommandHandler : ICommandHandler<CreateNotificationTemplateCommand, AdminNotificationTemplateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateNotificationTemplateCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationTemplateDto> Handle(CreateNotificationTemplateCommand command, CancellationToken cancellationToken = default)
        {
            var existing = await _db.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == command.Name, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Notification template with this name already exists");

            var template = new NotificationTemplate
            {
                Name = command.Name,
                Channel = command.Channel,
                SubjectTemplate = command.SubjectTemplate,
                BodyTemplate = command.BodyTemplate,
                VariablesJson = command.VariablesJson,
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.NotificationTemplates.Add(template);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminNotificationTemplateDto>(template);
        }
    }

    public class UpdateNotificationTemplateCommandHandler : ICommandHandler<UpdateNotificationTemplateCommand, AdminNotificationTemplateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateNotificationTemplateCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationTemplateDto> Handle(UpdateNotificationTemplateCommand command, CancellationToken cancellationToken = default)
        {
            var template = await _db.NotificationTemplates.FindAsync(new object[] { command.Id }, cancellationToken);
            if (template == null)
                throw new Domain.Exceptions.NotFoundException("NotificationTemplate", command.Id);

            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(template);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            var existing = await _db.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == command.Name && t.Id != command.Id, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Notification template with this name already exists");

            template.Name = command.Name;
            template.Channel = command.Channel;
            template.SubjectTemplate = command.SubjectTemplate;
            template.BodyTemplate = command.BodyTemplate;
            template.VariablesJson = command.VariablesJson;
            template.IsActive = command.IsActive;
            template.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminNotificationTemplateDto>(template);
        }
    }

    public class DeleteNotificationTemplateCommandHandler : ICommandHandler<DeleteNotificationTemplateCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteNotificationTemplateCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteNotificationTemplateCommand command, CancellationToken cancellationToken = default)
        {
            var template = await _db.NotificationTemplates.FindAsync(new object[] { command.Id }, cancellationToken);
            if (template == null)
                throw new Domain.Exceptions.NotFoundException("NotificationTemplate", command.Id);

            _db.NotificationTemplates.Remove(template);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class UpdateNotificationPreferenceCommandHandler : ICommandHandler<UpdateNotificationPreferenceCommand, AdminNotificationPreferenceDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateNotificationPreferenceCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationPreferenceDto> Handle(UpdateNotificationPreferenceCommand command, CancellationToken cancellationToken = default)
        {
            var preference = await _db.NotificationPreferences.FindAsync(new object[] { command.Id }, cancellationToken);
            if (preference == null)
                throw new Domain.Exceptions.NotFoundException("NotificationPreference", command.Id);

            preference.IsEnabled = command.IsEnabled;
            preference.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminNotificationPreferenceDto>(preference);
        }
    }

    public class CreateNotificationChannelCommandHandler : ICommandHandler<CreateNotificationChannelCommand, AdminNotificationChannelDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateNotificationChannelCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationChannelDto> Handle(CreateNotificationChannelCommand command, CancellationToken cancellationToken = default)
        {
            var existing = await _db.NotificationChannels
                .FirstOrDefaultAsync(c => c.Name == command.Name, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Notification channel with this name already exists");

            var channel = new NotificationChannel
            {
                Name = command.Name,
                Provider = command.Provider,
                ConfigurationJson = command.ConfigurationJson,
                IsActive = command.IsActive,
                Priority = command.Priority,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.NotificationChannels.Add(channel);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminNotificationChannelDto>(channel);
        }
    }

    public class UpdateNotificationChannelCommandHandler : ICommandHandler<UpdateNotificationChannelCommand, AdminNotificationChannelDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateNotificationChannelCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminNotificationChannelDto> Handle(UpdateNotificationChannelCommand command, CancellationToken cancellationToken = default)
        {
            var channel = await _db.NotificationChannels.FindAsync(new object[] { command.Id }, cancellationToken);
            if (channel == null)
                throw new Domain.Exceptions.NotFoundException("NotificationChannel", command.Id);

            var existing = await _db.NotificationChannels
                .FirstOrDefaultAsync(c => c.Name == command.Name && c.Id != command.Id, cancellationToken);
            if (existing != null)
                throw new Domain.Exceptions.DomainException("Notification channel with this name already exists");

            channel.Name = command.Name;
            channel.Provider = command.Provider;
            channel.ConfigurationJson = command.ConfigurationJson;
            channel.IsActive = command.IsActive;
            channel.Priority = command.Priority;
            channel.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminNotificationChannelDto>(channel);
        }
    }

    public class DeleteNotificationChannelCommandHandler : ICommandHandler<DeleteNotificationChannelCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteNotificationChannelCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteNotificationChannelCommand command, CancellationToken cancellationToken = default)
        {
            var channel = await _db.NotificationChannels.FindAsync(new object[] { command.Id }, cancellationToken);
            if (channel == null)
                throw new Domain.Exceptions.NotFoundException("NotificationChannel", command.Id);

            _db.NotificationChannels.Remove(channel);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}