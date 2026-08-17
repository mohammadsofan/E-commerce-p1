using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreatePromotionCommandHandler : ICommandHandler<CreatePromotionCommand, AdminPromotionDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreatePromotionCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminPromotionDto> Handle(CreatePromotionCommand command, CancellationToken cancellationToken = default)
        {
            var promotion = new Promotion
            {
                Name = command.Name,
                Description = command.Description,
                Type = command.Type,
                RulesJson = command.RulesJson,
                StartAt = command.StartAt,
                EndAt = command.EndAt,
                IsActive = command.IsActive,
                Priority = command.Priority,
                AllowCombine = command.AllowCombine,
                ApplicableProductIds = command.ApplicableProductIds,
                ApplicableCategoryIds = command.ApplicableCategoryIds,
                ApplicableUserIds = command.ApplicableUserIds,
                ExcludedProductIds = command.ExcludedProductIds,
                ExcludedCategoryIds = command.ExcludedCategoryIds,
                UsageLimit = command.UsageLimit,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.Promotions.Add(promotion);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminPromotionDto>(promotion);
        }
    }

    public class UpdatePromotionCommandHandler : ICommandHandler<UpdatePromotionCommand, AdminPromotionDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdatePromotionCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminPromotionDto> Handle(UpdatePromotionCommand command, CancellationToken cancellationToken = default)
        {
            var promotion = await _db.Promotions.FindAsync(new object[] { command.Id }, cancellationToken);

            if (promotion == null)
                throw new Domain.Exceptions.NotFoundException("Promotion", command.Id);

            // Optimistic concurrency check
            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(promotion);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            promotion.Name = command.Name;
            promotion.Description = command.Description;
            promotion.Type = command.Type;
            promotion.RulesJson = command.RulesJson;
            promotion.StartAt = command.StartAt;
            promotion.EndAt = command.EndAt;
            promotion.IsActive = command.IsActive;
            promotion.Priority = command.Priority;
            promotion.AllowCombine = command.AllowCombine;
            promotion.ApplicableProductIds = command.ApplicableProductIds;
            promotion.ApplicableCategoryIds = command.ApplicableCategoryIds;
            promotion.ApplicableUserIds = command.ApplicableUserIds;
            promotion.ExcludedProductIds = command.ExcludedProductIds;
            promotion.ExcludedCategoryIds = command.ExcludedCategoryIds;
            promotion.UsageLimit = command.UsageLimit;
            promotion.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminPromotionDto>(promotion);
        }
    }

    public class DeletePromotionCommandHandler : ICommandHandler<DeletePromotionCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeletePromotionCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeletePromotionCommand command, CancellationToken cancellationToken = default)
        {
            var promotion = await _db.Promotions
                .Include(p => p.Usages)
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (promotion == null)
                throw new Domain.Exceptions.NotFoundException("Promotion", command.Id);

            if (promotion.Usages.Any())
                throw new Domain.Exceptions.DomainException("Cannot delete promotion that has been used");

            _db.Promotions.Remove(promotion);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}