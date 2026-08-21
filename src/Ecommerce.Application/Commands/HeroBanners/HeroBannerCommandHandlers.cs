using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.HeroBanners
{
    public class CreateHeroBannerCommandHandler : ICommandHandler<CreateHeroBannerCommand, HeroBannerDto>
    {
        private readonly IApplicationDbContext _db;

        public CreateHeroBannerCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<HeroBannerDto> Handle(CreateHeroBannerCommand command, CancellationToken cancellationToken = default)
        {
            var banner = new HeroBanner
            {
                Id = Guid.NewGuid(),
                BadgeText = command.BadgeText?.Trim() ?? string.Empty,
                Title = command.Title?.Trim() ?? string.Empty,
                Subtitle = command.Subtitle?.Trim() ?? string.Empty,
                PrimaryButtonText = command.PrimaryButtonText?.Trim() ?? string.Empty,
                PrimaryButtonLink = command.PrimaryButtonLink?.Trim() ?? string.Empty,
                SecondaryButtonText = command.SecondaryButtonText?.Trim() ?? string.Empty,
                SecondaryButtonLink = command.SecondaryButtonLink?.Trim() ?? string.Empty,
                ImageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim(),
                IsActive = command.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _db.HeroBanners.AddAsync(banner, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new HeroBannerDto
            {
                Id = banner.Id,
                BadgeText = banner.BadgeText,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                PrimaryButtonText = banner.PrimaryButtonText,
                PrimaryButtonLink = banner.PrimaryButtonLink,
                SecondaryButtonText = banner.SecondaryButtonText,
                SecondaryButtonLink = banner.SecondaryButtonLink,
                ImageUrl = banner.ImageUrl,
                IsActive = banner.IsActive,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            };
        }
    }

    public class UpdateHeroBannerCommandHandler : ICommandHandler<UpdateHeroBannerCommand, HeroBannerDto>
    {
        private readonly IApplicationDbContext _db;

        public UpdateHeroBannerCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<HeroBannerDto> Handle(UpdateHeroBannerCommand command, CancellationToken cancellationToken = default)
        {
            var banner = await _db.HeroBanners
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (banner == null)
            {
                throw new NotFoundException("HeroBanner", command.Id);
            }

            banner.BadgeText = command.BadgeText?.Trim() ?? string.Empty;
            banner.Title = command.Title?.Trim() ?? string.Empty;
            banner.Subtitle = command.Subtitle?.Trim() ?? string.Empty;
            banner.PrimaryButtonText = command.PrimaryButtonText?.Trim() ?? string.Empty;
            banner.PrimaryButtonLink = command.PrimaryButtonLink?.Trim() ?? string.Empty;
            banner.SecondaryButtonText = command.SecondaryButtonText?.Trim() ?? string.Empty;
            banner.SecondaryButtonLink = command.SecondaryButtonLink?.Trim() ?? string.Empty;
            banner.ImageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim();
            banner.IsActive = command.IsActive;
            banner.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new HeroBannerDto
            {
                Id = banner.Id,
                BadgeText = banner.BadgeText,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                PrimaryButtonText = banner.PrimaryButtonText,
                PrimaryButtonLink = banner.PrimaryButtonLink,
                SecondaryButtonText = banner.SecondaryButtonText,
                SecondaryButtonLink = banner.SecondaryButtonLink,
                ImageUrl = banner.ImageUrl,
                IsActive = banner.IsActive,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            };
        }
    }

    public class SetActiveHeroBannerCommandHandler : ICommandHandler<SetActiveHeroBannerCommand, HeroBannerDto>
    {
        private readonly IApplicationDbContext _db;

        public SetActiveHeroBannerCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<HeroBannerDto> Handle(SetActiveHeroBannerCommand command, CancellationToken cancellationToken = default)
        {
            var banner = await _db.HeroBanners
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (banner == null)
            {
                throw new NotFoundException("HeroBanner", command.Id);
            }

            banner.IsActive = !banner.IsActive;
            banner.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new HeroBannerDto
            {
                Id = banner.Id,
                BadgeText = banner.BadgeText,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                PrimaryButtonText = banner.PrimaryButtonText,
                PrimaryButtonLink = banner.PrimaryButtonLink,
                SecondaryButtonText = banner.SecondaryButtonText,
                SecondaryButtonLink = banner.SecondaryButtonLink,
                ImageUrl = banner.ImageUrl,
                IsActive = banner.IsActive,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            };
        }
    }

    public class DeleteHeroBannerCommandHandler : ICommandHandler<DeleteHeroBannerCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteHeroBannerCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteHeroBannerCommand command, CancellationToken cancellationToken = default)
        {
            var banner = await _db.HeroBanners
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (banner == null)
            {
                throw new NotFoundException("HeroBanner", command.Id);
            }

            _db.HeroBanners.Remove(banner);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
