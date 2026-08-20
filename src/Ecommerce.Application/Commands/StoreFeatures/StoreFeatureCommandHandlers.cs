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

namespace Ecommerce.Application.Commands.StoreFeatures
{
    public class CreateStoreFeatureCommandHandler : ICommandHandler<CreateStoreFeatureCommand, StoreFeatureDto>
    {
        private readonly IApplicationDbContext _db;

        public CreateStoreFeatureCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StoreFeatureDto> Handle(CreateStoreFeatureCommand command, CancellationToken cancellationToken = default)
        {
            var feature = new StoreFeature
            {
                Id = Guid.NewGuid(),
                Title = command.Title.Trim(),
                Description = command.Description.Trim(),
                IconName = string.IsNullOrWhiteSpace(command.IconName) ? "Truck" : command.IconName.Trim(),
                DisplayOrder = command.DisplayOrder,
                IsActive = command.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.StoreFeatures.Add(feature);
            await _db.SaveChangesAsync(cancellationToken);

            return new StoreFeatureDto
            {
                Id = feature.Id,
                Title = feature.Title,
                Description = feature.Description,
                IconName = feature.IconName,
                DisplayOrder = feature.DisplayOrder,
                IsActive = feature.IsActive,
                CreatedAt = feature.CreatedAt,
                UpdatedAt = feature.UpdatedAt
            };
        }
    }

    public class UpdateStoreFeatureCommandHandler : ICommandHandler<UpdateStoreFeatureCommand, StoreFeatureDto>
    {
        private readonly IApplicationDbContext _db;

        public UpdateStoreFeatureCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StoreFeatureDto> Handle(UpdateStoreFeatureCommand command, CancellationToken cancellationToken = default)
        {
            var feature = await _db.StoreFeatures.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);
            if (feature == null)
            {
                throw new NotFoundException("StoreFeature", command.Id);
            }

            feature.Title = command.Title.Trim();
            feature.Description = command.Description.Trim();
            feature.IconName = string.IsNullOrWhiteSpace(command.IconName) ? "Truck" : command.IconName.Trim();
            feature.DisplayOrder = command.DisplayOrder;
            feature.IsActive = command.IsActive;
            feature.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new StoreFeatureDto
            {
                Id = feature.Id,
                Title = feature.Title,
                Description = feature.Description,
                IconName = feature.IconName,
                DisplayOrder = feature.DisplayOrder,
                IsActive = feature.IsActive,
                CreatedAt = feature.CreatedAt,
                UpdatedAt = feature.UpdatedAt
            };
        }
    }

    public class DeleteStoreFeatureCommandHandler : ICommandHandler<DeleteStoreFeatureCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteStoreFeatureCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteStoreFeatureCommand command, CancellationToken cancellationToken = default)
        {
            var feature = await _db.StoreFeatures.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);
            if (feature == null)
            {
                throw new NotFoundException("StoreFeature", command.Id);
            }

            _db.StoreFeatures.Remove(feature);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
